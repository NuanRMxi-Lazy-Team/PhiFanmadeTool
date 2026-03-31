using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件拟合器：将连续线性事件拟合为更少的缓动事件，并保留原有非线性事件不变。
/// </summary>
internal static class EventFit
{
    private const int MinEasingId = 1;
    private const int MaxEasingId = 31;
    private const double NumericEpsilon = 1e-9;

    /// <summary>
    /// 对事件列表执行缓动拟合。仅会拟合连续、线性、数值型的事件段；原有非线性事件会被原样保留。
    /// </summary>
    internal static List<Nrc.Event<T>> EventListFit<T>(
        List<Nrc.Event<T>>? events,
        double precision = 64d,
        double tolerance = 5d)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (precision <= 0)
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be greater than 0.");

        EnsureSupportedNumericType<T>();

        if (events == null || events.Count == 0)
            return [];

        NrcToolLog.OnInfo($"EventListFit: 开始拟合，共 {events.Count} 个事件，精度={precision}，容差={tolerance}%");

        var sortedEvents = events
            .Select(e => e.Clone())
            .OrderBy(e => e.StartBeat)
            .ThenBy(e => e.EndBeat)
            .ToList();

        var result = new List<Nrc.Event<T>>(sortedEvents.Count);

        for (var index = 0; index < sortedEvents.Count;)
        {
            if (!CanParticipateInFit(sortedEvents[index]))
            {
                var skipped = sortedEvents[index];
                NrcToolLog.OnDebug(
                    $"EventListFit: 跳过非线性事件 [{skipped.StartBeat} -> {skipped.EndBeat}]，" +
                    $"缓动={(int)skipped.Easing}，贝塞尔={skipped.IsBezier}");
                result.Add(skipped);
                index++;
                continue;
            }

            var runEnd = index + 1;
            while (runEnd < sortedEvents.Count && CanAppendToFitRun(sortedEvents[runEnd - 1], sortedEvents[runEnd], tolerance))
            {
                runEnd++;
            }

            if (runEnd - index < 2)
            {
                NrcToolLog.OnDebug(
                    $"EventListFit: 线性段过短（1 个事件），直接保留 " +
                    $"[{sortedEvents[index].StartBeat} -> {sortedEvents[index].EndBeat}]");
                result.Add(sortedEvents[index]);
                index++;
                continue;
            }

            NrcToolLog.OnDebug(
                $"EventListFit: 发现可拟合线性段 " +
                $"[{sortedEvents[index].StartBeat} -> {sortedEvents[runEnd - 1].EndBeat}]，" +
                $"共 {runEnd - index} 个事件");

            result.AddRange(FitLinearRun(sortedEvents, index, runEnd, precision, tolerance));
            index = runEnd;
        }

        NrcToolLog.OnInfo($"EventListFit: 拟合完成，{events.Count} → {result.Count} 个事件");
        return result;
    }

    private static List<Nrc.Event<T>> FitLinearRun<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endExclusive,
        double precision,
        double tolerance)
    {
        var result = new List<Nrc.Event<T>>();

        for (var index = startIndex; index < endExclusive;)
        {
            var fitted = TryFitLongestRange(runEvents, index, endExclusive, precision, tolerance);
            if (fitted is null)
            {
                NrcToolLog.OnDebug(
                    $"EventListFit: 段 [{runEvents[index].StartBeat} -> {runEvents[index].EndBeat}] " +
                    $"无法拟合，保留原事件");
                result.Add(runEvents[index]);
                index++;
                continue;
            }

            NrcToolLog.OnInfo(
                $"EventListFit: 拟合成功 [{fitted.Value.Event.StartBeat} -> {fitted.Value.Event.EndBeat}]，" +
                $"{fitted.Value.Length} 个事件 → 1 个事件，缓动={(int)fitted.Value.Event.Easing}");
            result.Add(fitted.Value.Event);
            index += fitted.Value.Length;
        }

        return result;
    }

    private static (Nrc.Event<T> Event, int Length)? TryFitLongestRange<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endExclusive,
        double precision,
        double tolerance)
    {
        for (var endIndex = endExclusive - 1; endIndex > startIndex; endIndex--)
        {
            if (!TryCreateBestFittedEvent(runEvents, startIndex, endIndex, precision, tolerance, out var fittedEvent))
                continue;

            return (fittedEvent, endIndex - startIndex + 1);
        }

        return null;
    }

    private static bool TryCreateBestFittedEvent<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endIndex,
        double precision,
        double tolerance,
        out Nrc.Event<T> fittedEvent)
    {
        var samples = BuildSamples(runEvents, startIndex, endIndex, precision);
        if (samples.Count < 2)
        {
            fittedEvent = runEvents[startIndex].Clone();
            return false;
        }

        var errorScale = GetErrorScale(samples);
        var sourceProfile = AnalyzeSourceProfile(samples);

        var first = runEvents[startIndex];
        var last  = runEvents[endIndex];

        var startValue = Convert.ToDouble(first.StartValue);
        var endValue = Convert.ToDouble(last.EndValue);

        if (Math.Abs(endValue - startValue) <= NumericEpsilon)
        {
            // Flat segments do not have meaningful IN/OUT phase; keep them linear only.
            var linear = CreateCandidateEvent(first, last, MinEasingId);
            if (TryMeasureCandidateError(linear, samples, errorScale, tolerance, out _, out _))
            {
                fittedEvent = linear;
                return true;
            }

            fittedEvent = first.Clone();
            return false;
        }

        Nrc.Event<T>? bestCandidate = null;
        var bestScore = double.PositiveInfinity;

        for (var easingId = MinEasingId; easingId <= MaxEasingId; easingId++)
        {
            var candidate = CreateCandidateEvent(first, last, easingId);
            if (!TryScoreCandidate(candidate, samples, sourceProfile, errorScale, tolerance, out var candidateScore))
                continue;

            if (candidateScore > bestScore + NumericEpsilon)
                continue;

            if (Math.Abs(candidateScore - bestScore) <= NumericEpsilon && bestCandidate is not null)
            {
                if ((int)candidate.Easing >= (int)bestCandidate.Easing)
                    continue;
            }

            bestScore = candidateScore;
            bestCandidate = candidate;
        }

        if (bestCandidate is null)
        {
            fittedEvent = first.Clone();
            return false;
        }

        fittedEvent = bestCandidate;
        return true;
    }

    private static List<SamplePoint> BuildSamples<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endIndex,
        double precision)
    {
        var first = runEvents[startIndex];
        var last = runEvents[endIndex];

        var globalStartBeat = first.StartBeat;
        var globalEndBeat = last.EndBeat;
        var totalBeatSpan = (double)(globalEndBeat - globalStartBeat);
        var startValue = Convert.ToDouble(first.StartValue);
        var endValue = Convert.ToDouble(last.EndValue);
        var totalValueDelta = endValue - startValue;

        var samples = new List<SamplePoint>();

        for (var index = startIndex; index <= endIndex; index++)
        {
            var currentEvent = runEvents[index];
            AddSample(
                samples,
                globalStartBeat,
                totalBeatSpan,
                startValue,
                totalValueDelta,
                currentEvent.StartBeat,
                Convert.ToDouble(currentEvent.StartValue));

            var segmentCount = Math.Max(2, (int)Math.Ceiling((double)(currentEvent.EndBeat - currentEvent.StartBeat) * precision));
            for (var step = 1; step < segmentCount; step++)
            {
                var ratio = step / (double)segmentCount;
                var beat  = LerpBeat(currentEvent.StartBeat, currentEvent.EndBeat, ratio);
                AddSample(
                    samples,
                    globalStartBeat,
                    totalBeatSpan,
                    startValue,
                    totalValueDelta,
                    beat,
                    Convert.ToDouble(currentEvent.GetValueAtBeat(beat)));
            }

            AddSample(
                samples,
                globalStartBeat,
                totalBeatSpan,
                startValue,
                totalValueDelta,
                currentEvent.EndBeat,
                Convert.ToDouble(currentEvent.EndValue));
        }

        return samples;
    }

    private static void AddSample(
        List<SamplePoint> samples,
        Beat globalStartBeat,
        double totalBeatSpan,
        double startValue,
        double totalValueDelta,
        Beat beat,
        double value)
    {
        var normalizedTime = totalBeatSpan <= NumericEpsilon ? 0d : ((double)(beat - globalStartBeat)) / totalBeatSpan;
        var progress = Math.Abs(totalValueDelta) <= NumericEpsilon ? 0d : (value - startValue) / totalValueDelta;

        if (samples.Count != 0 && samples[^1].Beat == beat)
        {
            samples[^1] = new SamplePoint(beat, normalizedTime, value, progress);
            return;
        }

        samples.Add(new SamplePoint(beat, normalizedTime, value, progress));
    }

    private static Beat LerpBeat(Beat startBeat, Beat endBeat, double t)
        => new((double)startBeat + ((double)endBeat - (double)startBeat) * t);

    private static double GetErrorScale(List<SamplePoint> samples)
    {
        var maxAbsValue = samples.Count == 0 ? 0d : samples.Max(sample => Math.Abs(sample.Value));
        return Math.Max(maxAbsValue, 1d);
    }

    private static bool TryScoreCandidate<T>(
        Nrc.Event<T> candidate,
        List<SamplePoint> samples,
        SourceProfile sourceProfile,
        double errorScale,
        double tolerance,
        out double score)
    {
        if (!TryMeasureCandidateError(candidate, samples, errorScale, tolerance, out var normalizedMaxError, out var normalizedRmse))
        {
            score = double.PositiveInfinity;
            return false;
        }

        var candidateProfile = AnalyzeCandidateProfile(candidate, samples, sourceProfile.TotalValueDelta);

        if (!sourceProfile.HasOvershootValue && candidateProfile.HasOvershootValue)
        {
            score = double.PositiveInfinity;
            return false;
        }

        if (sourceProfile.IsMonotonicValue && !candidateProfile.IsMonotonicValue)
        {
            score = double.PositiveInfinity;
            return false;
        }

        var sourcePhase = sourceProfile.Phase;
        var candidatePhase = GetEasingPhase((int)candidate.Easing);
        if (sourcePhase is not EasingPhase.Unknown && sourcePhase != candidatePhase)
        {
            score = double.PositiveInfinity;
            return false;
        }

        var semanticDistance =
            Math.Abs(sourceProfile.Progress25 - candidateProfile.Progress25) +
            Math.Abs(sourceProfile.Progress50 - candidateProfile.Progress50) +
            Math.Abs(sourceProfile.Progress75 - candidateProfile.Progress75);

        score = normalizedMaxError * 100d + normalizedRmse * 40d + semanticDistance * 4d;
        return true;
    }

    private static bool TryMeasureCandidateError<T>(
        Nrc.Event<T> candidate,
        List<SamplePoint> samples,
        double errorScale,
        double tolerance,
        out double normalizedMaxError,
        out double normalizedRmse)
    {
        var allowedError = tolerance / 100d * errorScale;
        var maxError     = 0d;
        var sumSquaredError = 0d;

        foreach (var sample in samples)
        {
            var candidateValue = Convert.ToDouble(candidate.GetValueAtBeat(sample.Beat));
            var error          = Math.Abs(candidateValue - sample.Value);
            if (error > allowedError)
            {
                normalizedMaxError = double.PositiveInfinity;
                normalizedRmse = double.PositiveInfinity;
                return false;
            }

            if (error > maxError)
                maxError = error;

            sumSquaredError += error * error;
        }

        normalizedMaxError = maxError / errorScale;
        normalizedRmse = Math.Sqrt(sumSquaredError / samples.Count) / errorScale;
        return true;
    }

    private static SourceProfile AnalyzeSourceProfile(List<SamplePoint> samples)
    {
        var p25 = GetProgressAtTime(samples, 0.25d);
        var p50 = GetProgressAtTime(samples, 0.50d);
        var p75 = GetProgressAtTime(samples, 0.75d);

        return new SourceProfile(
            p25,
            p50,
            p75,
            DetectPhase(p25, p75),
            HasOvershoot(samples.Select(s => s.Progress)),
            IsMonotonic(samples.Select(s => s.Progress)),
            samples[^1].Value - samples[0].Value);
    }

    private static CandidateProfile AnalyzeCandidateProfile<T>(
        Nrc.Event<T> candidate,
        List<SamplePoint> samples,
        double totalValueDelta)
    {
        var startValue = Convert.ToDouble(candidate.StartValue);
        var progresses = new List<double>(samples.Count);

        foreach (var sample in samples)
        {
            var candidateValue = Convert.ToDouble(candidate.GetValueAtBeat(sample.Beat));
            var progress = Math.Abs(totalValueDelta) <= NumericEpsilon
                ? 0d
                : (candidateValue - startValue) / totalValueDelta;
            progresses.Add(progress);
        }

        return new CandidateProfile(
            GetProgressAtTime(samples, progresses, 0.25d),
            GetProgressAtTime(samples, progresses, 0.50d),
            GetProgressAtTime(samples, progresses, 0.75d),
            HasOvershoot(progresses),
            IsMonotonic(progresses));
    }

    private static double GetProgressAtTime(List<SamplePoint> samples, double targetTime)
        => GetProgressAtTime(samples, samples.Select(s => s.Progress).ToList(), targetTime);

    private static double GetProgressAtTime(List<SamplePoint> samples, List<double> progresses, double targetTime)
    {
        if (samples.Count == 0)
            return 0d;

        if (targetTime <= samples[0].Time)
            return progresses[0];

        for (var i = 1; i < samples.Count; i++)
        {
            if (samples[i].Time < targetTime)
                continue;

            var left = samples[i - 1];
            var right = samples[i];
            var span = right.Time - left.Time;
            if (span <= NumericEpsilon)
                return progresses[i];

            var ratio = (targetTime - left.Time) / span;
            return progresses[i - 1] + (progresses[i] - progresses[i - 1]) * ratio;
        }

        return progresses[^1];
    }

    private static bool HasOvershoot(IEnumerable<double> progresses)
    {
        foreach (var progress in progresses)
        {
            if (progress < -0.01d || progress > 1.01d)
                return true;
        }

        return false;
    }

    private static bool IsMonotonic(IEnumerable<double> progresses)
    {
        var first = true;
        var previous = 0d;

        foreach (var progress in progresses)
        {
            if (first)
            {
                previous = progress;
                first = false;
                continue;
            }

            if (progress + 1e-4d < previous)
                return false;

            previous = progress;
        }

        return true;
    }

    private static EasingPhase DetectPhase(double p25, double p75)
    {
        const double epsilon = 0.015d;

        var d25 = p25 - 0.25d;
        var d75 = p75 - 0.75d;

        if (Math.Abs(d25) <= epsilon && Math.Abs(d75) <= epsilon)
            return EasingPhase.Linear;

        if (d25 < -epsilon && d75 < -epsilon)
            return EasingPhase.In;

        if (d25 > epsilon && d75 > epsilon)
            return EasingPhase.Out;

        if (d25 < -epsilon && d75 > epsilon)
            return EasingPhase.InOut;

        return EasingPhase.Unknown;
    }

    private static EasingPhase GetEasingPhase(int easingId)
    {
        if (easingId <= 1)
            return EasingPhase.Linear;

        var offset = (easingId - 2) % 3;
        return offset switch
        {
            0 => EasingPhase.In,
            1 => EasingPhase.Out,
            2 => EasingPhase.InOut,
            _ => EasingPhase.Unknown
        };
    }

    private static Nrc.Event<T> CreateCandidateEvent<T>(Nrc.Event<T> first, Nrc.Event<T> last, int easingId)
        => new()
        {
            StartBeat  = new Beat((int[])first.StartBeat),
            EndBeat    = new Beat((int[])last.EndBeat),
            StartValue = first.StartValue,
            EndValue   = last.EndValue,
            Easing     = new Nrc.Easing(easingId),
            Font       = first.Font
        };

    private static bool CanParticipateInFit<T>(Nrc.Event<T> evt)
        => evt.EndBeat > evt.StartBeat &&
           !evt.IsBezier &&
           evt.Easing == 1;

    private static bool CanAppendToFitRun<T>(
        Nrc.Event<T> previousEvent,
        Nrc.Event<T> currentEvent,
        double tolerance)
    {
        if (!CanParticipateInFit(currentEvent))
            return false;
        if (previousEvent.EndBeat != currentEvent.StartBeat)
            return false;
        if (!string.Equals(previousEvent.Font, currentEvent.Font, StringComparison.Ordinal))
            return false;

        return AreClose(Convert.ToDouble(previousEvent.EndValue), Convert.ToDouble(currentEvent.StartValue), tolerance);
    }

    private static bool AreClose(double left, double right, double tolerance)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), 1d);
        return Math.Abs(left - right) <= tolerance / 100d * scale;
    }

    private static void EnsureSupportedNumericType<T>()
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListFit only supports int, float, and double types.");
    }

    private readonly record struct SamplePoint(Beat Beat, double Time, double Value, double Progress);

    private readonly record struct SourceProfile(
        double Progress25,
        double Progress50,
        double Progress75,
        EasingPhase Phase,
        bool HasOvershootValue,
        bool IsMonotonicValue,
        double TotalValueDelta);

    private readonly record struct CandidateProfile(
        double Progress25,
        double Progress50,
        double Progress75,
        bool HasOvershootValue,
        bool IsMonotonicValue);

    private enum EasingPhase
    {
        Unknown,
        Linear,
        In,
        Out,
        InOut
    }
}