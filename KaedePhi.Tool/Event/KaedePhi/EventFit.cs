using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

public class EventFit<TPayload> : IEventFit<Kpc.Event<TPayload>>
{
    private const int MinEasingId = 1;
    private const int MaxEasingId = 31;
    private const double NumericEpsilon = 1e-9;

    // DP 中每增加一个段的惩罚；越大越倾向于更少事件。
    private const double SegmentPenalty = 1.0d;

    // 保留原事件的轻微偏置，避免在边界条件下过拟合复杂缓动。
    private const double KeepOriginalPenalty = 1.02d;

    // 长段时限制回看窗口，避免 O(n^2) 爆炸。
    private const int FullSearchRunLengthThreshold = 160;
    private const int LongRunSearchWindow = 160;

    public Action<string> OnInfo { get; set; } = _ => { };
    public Action<string> OnWarning { get; set; } = _ => { };
    public Action<string> OnError { get; set; } = _ => { };
    private void Info(string message) => OnInfo.Invoke(message);
    private void Warning(string message) => OnWarning.Invoke(message);
    private void Error(string message) => OnError.Invoke(message);


    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventListFit(
        List<Kpc.Event<TPayload>>? events,
        double tolerance)
        => EventListFitCore(events, tolerance, null, CancellationToken.None);

    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventListFit(
        List<Kpc.Event<TPayload>>? events,
        double tolerance,
        int? maxDegreeOfParallelism)
        => EventListFitCore(events, tolerance, maxDegreeOfParallelism, CancellationToken.None);

    /// <summary>
    /// 对事件列表执行异步拟合。
    /// </summary>
    private Task<List<Kpc.Event<TPayload>>> EventListFitAsync(
        List<Kpc.Event<TPayload>>? events,
        double tolerance = 5d,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
        => Task.Run(() => EventListFitCore(events, tolerance, maxDegreeOfParallelism, cancellationToken),
            cancellationToken);

    private List<Kpc.Event<TPayload>> EventListFitCore(
        List<Kpc.Event<TPayload>>? events,
        double tolerance,
        int? maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");

        EnsureSupportedNumericType();

        if (events == null || events.Count == 0)
            return [];

        var degree = ResolveMaxDegreeOfParallelism(maxDegreeOfParallelism);
        OnInfo($"EventListFit: 开始拟合，共 {events.Count} 个事件，容差={tolerance}% ，并行度={degree}");

        var sortedEvents = events
            .Select(e => e.Clone())
            .OrderBy(e => e.StartBeat)
            .ThenBy(e => e.EndBeat)
            .ToList();

        var units = BuildFitUnits(sortedEvents, tolerance);
        var outputs = new List<Kpc.Event<TPayload>>[units.Count];

        if (units is [{ NeedsFit: true }])
        {
            // 单个超长 run 时，把所有核心预算给 run 内部 DP 并行。
            outputs[0] = FitLinearRun(
                sortedEvents,
                units[0].Start,
                units[0].EndExclusive,
                tolerance,
                degree,
                cancellationToken);
        }
        else
        {
            // 多个 run 时并行 run，避免 run 内外嵌套并行导致线程池抖动。
            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = degree
            };

            Parallel.For(0, units.Count, options, unitIndex =>
            {
                var unit = units[unitIndex];
                if (!unit.NeedsFit)
                {
                    outputs[unitIndex] = [sortedEvents[unit.Start].Clone()];
                    return;
                }

                outputs[unitIndex] = FitLinearRun(
                    sortedEvents,
                    unit.Start,
                    unit.EndExclusive,
                    tolerance,
                    1,
                    cancellationToken);
            });
        }

        var result = new List<Kpc.Event<TPayload>>(sortedEvents.Count);
        foreach (var t in outputs)
            result.AddRange(t);

        Info($"EventListFit: 拟合完成，{events.Count} -> {result.Count} 个事件");
        return result;
    }

    private List<FitUnit> BuildFitUnits(List<Kpc.Event<TPayload>> sortedEvents, double tolerance)
    {
        var units = new List<FitUnit>(sortedEvents.Count);

        for (var index = 0; index < sortedEvents.Count;)
        {
            if (!CanParticipateInFit(sortedEvents[index]))
            {
                units.Add(new FitUnit(index, index + 1, false));
                index++;
                continue;
            }

            var runEnd = index + 1;
            while (runEnd < sortedEvents.Count &&
                   CanAppendToFitRun(sortedEvents[runEnd - 1], sortedEvents[runEnd], tolerance))
            {
                runEnd++;
            }

            if (runEnd - index < 2)
            {
                units.Add(new FitUnit(index, index + 1, false));
                index++;
                continue;
            }

            Info(
                $"EventListFit: 发现可拟合线性段 [{sortedEvents[index].StartBeat} -> {sortedEvents[runEnd - 1].EndBeat}]，共 {runEnd - index} 个事件");
            units.Add(new FitUnit(index, runEnd, true));
            index = runEnd;
        }

        return units;
    }

    /// <summary>
    /// 对单个连续线性 run 执行拟合：并行预计算候选段，随后顺序 DP 选择最优分段。
    /// </summary>
    private List<Kpc.Event<TPayload>> FitLinearRun(
        List<Kpc.Event<TPayload>> runEvents,
        int startIndex,
        int endExclusive,
        double tolerance,
        int degree,
        CancellationToken cancellationToken)
    {
        var runLength = endExclusive - startIndex;
        var window = runLength <= FullSearchRunLengthThreshold ? runLength : LongRunSearchWindow;

        if (runLength > FullSearchRunLengthThreshold)
            Info(
                $"EventListFit: 长线性段优化模式启用，长度={runLength}，回看窗口={window}");

        // ---- Phase 1: 并行预计算所有候选段 ----
        // 所有 (start, end) 候选段的拟合结果完全独立于 DP 状态，可以一次性完全并行。
        // 避免旧方案"DP 步骤内反复启动并行任务"的线程池抖动，以及
        // ConcurrentDictionary.GetOrAdd 对同一 key 多次调用 factory 的冗余计算。
        var pairs = BuildSegmentPairs(runLength, window, startIndex);
        var fitResults = new FittedSegment?[pairs.Length];

        Parallel.For(0, pairs.Length, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = degree
        }, i =>
        {
            var (absStart, absEnd) = pairs[i];
            fitResults[i] = CreateFittedSegment(runEvents, absStart, absEnd, tolerance);
        });

        // 将并行结果整理为普通字典，Phase 2 顺序查找无锁开销
        var fitCache = new Dictionary<(int Start, int End), FittedSegment?>(pairs.Length);
        for (var i = 0; i < pairs.Length; i++)
            fitCache[pairs[i]] = fitResults[i];

        // ---- Phase 2: 顺序 DP，全部命中缓存，速度极快 ----
        var dpCost = new double[runLength + 1];
        var dpSegments = new int[runLength + 1];
        var prev = new int[runLength + 1];
        var chosen = new Kpc.Event<TPayload>?[runLength + 1];

        dpCost[0] = 0d;
        dpSegments[0] = 0;
        prev[0] = -1;
        chosen[0] = null;

        for (var i = 1; i <= runLength; i++)
        {
            dpCost[i] = double.PositiveInfinity;
            dpSegments[i] = int.MaxValue;
            prev[i] = -1;
            chosen[i] = null;
        }

        for (var endLocal = 1; endLocal <= runLength; endLocal++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bestPlan = CreateKeepPlan(runEvents, startIndex, endLocal, dpCost, dpSegments);
            bestPlan = ImprovePlanByFittedSegments(
                fitCache,
                startIndex,
                endLocal,
                window,
                dpCost,
                dpSegments,
                bestPlan);

            dpCost[endLocal] = bestPlan.Cost;
            dpSegments[endLocal] = bestPlan.Segments;
            prev[endLocal] = bestPlan.Prev;
            chosen[endLocal] = bestPlan.Event;
        }

        var reversed = new List<Kpc.Event<TPayload>>();
        for (var cursor = runLength; cursor > 0; cursor = prev[cursor])
        {
            var evt = chosen[cursor];
            if (evt == null)
                break;
            reversed.Add(evt);
        }

        reversed.Reverse();
        return reversed;
    }

    /// <summary>
    /// 以“保留最后一个原事件”作为 DP 初始方案。
    /// </summary>
    private PlanChoice CreateKeepPlan(
        List<Kpc.Event<TPayload>> runEvents,
        int startIndex,
        int endLocal,
        double[] dpCost,
        int[] dpSegments)
    {
        var absoluteEnd = startIndex + endLocal - 1;
        return new PlanChoice(
            dpCost[endLocal - 1] + KeepOriginalPenalty,
            dpSegments[endLocal - 1] + 1,
            endLocal - 1,
            runEvents[absoluteEnd].Clone());
    }

    /// <summary>
    /// 通过缓存的拟合段尝试改进当前最佳方案。
    /// </summary>
    private PlanChoice ImprovePlanByFittedSegments(
        IReadOnlyDictionary<(int Start, int End), FittedSegment?> fitCache,
        int startIndex,
        int endLocal,
        int window,
        double[] dpCost,
        int[] dpSegments,
        PlanChoice currentBest)
    {
        var absoluteEnd = startIndex + endLocal - 1;
        var startMin = Math.Max(0, endLocal - window);

        for (var startLocal = startMin; startLocal < endLocal - 1; startLocal++)
        {
            var absoluteStart = startIndex + startLocal;
            if (!fitCache.TryGetValue((absoluteStart, absoluteEnd), out var fitted) || fitted is null)
                continue;

            var candidate = new PlanChoice(
                dpCost[startLocal] + SegmentPenalty + fitted.Value.Score,
                dpSegments[startLocal] + 1,
                startLocal,
                fitted.Value.Event);

            if (IsBetterPlan(candidate, currentBest))
                currentBest = candidate;
        }

        return currentBest;
    }

    /// <summary>
    /// 枚举回看窗口内所有需要预计算的候选段对 (absoluteStart, absoluteEnd)。
    /// 段长至少覆盖 2 个原始事件，方可进行有意义的拟合。
    /// </summary>
    private (int AbsStart, int AbsEnd)[] BuildSegmentPairs(int runLength, int window, int startIndex)
    {
        // 先统计总数，一次性分配数组，避免 List 扩容
        var count = 0;
        for (var endLocal = 2; endLocal <= runLength; endLocal++)
        {
            var startMin = Math.Max(0, endLocal - window);
            count += endLocal - 1 - startMin; // startLocal 从 startMin 到 endLocal - 2
        }

        var pairs = new (int AbsStart, int AbsEnd)[count];
        var idx = 0;
        for (var endLocal = 2; endLocal <= runLength; endLocal++)
        {
            var startMin = Math.Max(0, endLocal - window);
            var absoluteEnd = startIndex + endLocal - 1;
            for (var startLocal = startMin; startLocal < endLocal - 1; startLocal++)
                pairs[idx++] = (startIndex + startLocal, absoluteEnd);
        }

        return pairs;
    }

    /// <summary>
    /// 比较两种 DP 方案优先级。
    /// 优先顺序：总成本 -> 分段数 -> 前驱位置（更长段）-> 缓动编号。
    /// </summary>
    private bool IsBetterPlan(PlanChoice candidate, PlanChoice currentBest)
    {
        if (candidate.Cost < currentBest.Cost - NumericEpsilon)
            return true;
        if (Math.Abs(candidate.Cost - currentBest.Cost) > NumericEpsilon)
            return false;

        if (candidate.Segments < currentBest.Segments)
            return true;
        if (candidate.Segments > currentBest.Segments)
            return false;

        // 相同代价和段数时优先更长段（更小 prev），保证 deterministic 且更压缩。
        if (candidate.Prev < currentBest.Prev)
            return true;
        if (candidate.Prev > currentBest.Prev)
            return false;

        return (int)candidate.Event.Easing < (int)currentBest.Event.Easing;
    }

    /// <summary>
    /// 构建并评分一个候选拟合段；若无可用缓动则返回 null。
    /// </summary>
    private FittedSegment? CreateFittedSegment(
        List<Kpc.Event<TPayload>> runEvents,
        int startIndex,
        int endIndex,
        double tolerance)
    {
        if (!TryCreateBestFittedEvent(runEvents, startIndex, endIndex, tolerance, out var fittedEvent, out var score))
            return null;

        return new FittedSegment(fittedEvent, score);
    }

    /// <summary>
    /// 在允许缓动集合中选择分数最低拟合事件。
    /// </summary>
    private bool TryCreateBestFittedEvent(
        List<Kpc.Event<TPayload>> runEvents,
        int startIndex,
        int endIndex,
        double tolerance,
        out Kpc.Event<TPayload> fittedEvent,
        out double bestScore)
    {
        var samples = BuildSamples(runEvents, startIndex, endIndex);
        if (samples.Count < 2)
        {
            fittedEvent = runEvents[startIndex].Clone();
            bestScore = double.PositiveInfinity;
            return false;
        }

        var sourceProfile = AnalyzeSourceProfile(samples);
        var errorScale = GetErrorScale(samples);

        var first = runEvents[startIndex];
        var last = runEvents[endIndex];
        var startValue = Convert.ToDouble(first.StartValue);
        var endValue = Convert.ToDouble(last.EndValue);

        // 平段仅允许线性，避免无意义的 IN/OUT 噪声。
        if (Math.Abs(endValue - startValue) <= NumericEpsilon)
        {
            var linear = CreateCandidateEvent(first, last, MinEasingId);
            if (TryScoreCandidate(linear, samples, sourceProfile, errorScale, tolerance, out bestScore))
            {
                fittedEvent = linear;
                return true;
            }

            fittedEvent = first.Clone();
            bestScore = double.PositiveInfinity;
            return false;
        }

        fittedEvent = first.Clone();
        bestScore = double.PositiveInfinity;
        var hasCandidate = false;

        for (var easingId = MinEasingId; easingId <= MaxEasingId; easingId++)
        {
            var candidatePhase = GetEasingPhase(easingId);
            if (sourceProfile.Phase is not EasingPhase.Unknown &&
                sourceProfile.Phase != candidatePhase &&
                candidatePhase != EasingPhase.Linear)
            {
                continue;
            }

            var candidate = CreateCandidateEvent(first, last, easingId);
            if (!TryScoreCandidate(candidate, samples, sourceProfile, errorScale, tolerance, out var candidateScore))
                continue;

            if (!hasCandidate || candidateScore < bestScore - NumericEpsilon ||
                (Math.Abs(candidateScore - bestScore) <= NumericEpsilon &&
                 (int)candidate.Easing < (int)fittedEvent.Easing))
            {
                fittedEvent = candidate;
                bestScore = candidateScore;
                hasCandidate = true;
            }
        }

        return hasCandidate;
    }

    private List<SamplePoint> BuildSamples(
        List<Kpc.Event<TPayload>> runEvents,
        int startIndex,
        int endIndex)
    {
        var samples = new List<SamplePoint>((endIndex - startIndex + 1) * 3);

        var first = runEvents[startIndex];
        var last = runEvents[endIndex];

        var globalStart = first.StartBeat;
        var globalEnd = last.EndBeat;
        var beatSpan = (double)(globalEnd - globalStart);

        var sourceStartValue = Convert.ToDouble(first.StartValue);
        var sourceEndValue = Convert.ToDouble(last.EndValue);
        var valueDelta = sourceEndValue - sourceStartValue;

        for (var i = startIndex; i <= endIndex; i++)
        {
            var evt = runEvents[i];

            AddSample(samples, globalStart, beatSpan, sourceStartValue, valueDelta,
                evt.StartBeat, Convert.ToDouble(evt.StartValue));

            var midBeat = LerpBeat(evt.StartBeat, evt.EndBeat, 0.5d);
            var midValue = Convert.ToDouble(evt.GetValueAtBeat(midBeat));
            AddSample(samples, globalStart, beatSpan, sourceStartValue, valueDelta, midBeat, midValue);

            AddSample(samples, globalStart, beatSpan, sourceStartValue, valueDelta,
                evt.EndBeat, Convert.ToDouble(evt.EndValue));
        }

        return samples;
    }

    private void AddSample(
        List<SamplePoint> samples,
        Beat globalStart,
        double beatSpan,
        double sourceStartValue,
        double valueDelta,
        Beat beat,
        double value)
    {
        var time = beatSpan <= NumericEpsilon ? 0d : ((double)(beat - globalStart)) / beatSpan;
        var progress = Math.Abs(valueDelta) <= NumericEpsilon ? 0d : (value - sourceStartValue) / valueDelta;

        if (samples.Count != 0 && samples[^1].Beat == beat)
        {
            samples[^1] = new SamplePoint(beat, time, value, progress);
            return;
        }

        samples.Add(new SamplePoint(beat, time, value, progress));
    }

    private double GetErrorScale(List<SamplePoint> samples)
    {
        var maxAbsValue = samples.Count == 0 ? 0d : samples.Max(s => Math.Abs(s.Value));
        return Math.Max(maxAbsValue, 1d);
    }

    /// <summary>
    /// 在容差约束下对候选事件打分；失败返回 <see langword="false"/>。
    /// </summary>
    private bool TryScoreCandidate(
        Kpc.Event<TPayload> candidate,
        List<SamplePoint> samples,
        SourceProfile sourceProfile,
        double errorScale,
        double tolerance,
        out double score)
    {
        if (!TryMeasureCandidateError(candidate, samples, errorScale, tolerance, out var normalizedMaxError,
                out var normalizedRmse))
        {
            score = double.PositiveInfinity;
            return false;
        }

        var candidateProfile = AnalyzeCandidateProfile(candidate, samples);

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

        var semanticDistance =
            Math.Abs(sourceProfile.Progress25 - candidateProfile.Progress25) +
            Math.Abs(sourceProfile.Progress50 - candidateProfile.Progress50) +
            Math.Abs(sourceProfile.Progress75 - candidateProfile.Progress75);

        score = normalizedMaxError * 90d + normalizedRmse * 35d + semanticDistance * 8d;
        return true;
    }

    private bool TryMeasureCandidateError(
        Kpc.Event<TPayload> candidate,
        List<SamplePoint> samples,
        double errorScale,
        double tolerance,
        out double normalizedMaxError,
        out double normalizedRmse)
    {
        var allowedError = tolerance / 100d * errorScale;
        var maxError = 0d;
        var sumSquaredError = 0d;

        foreach (var error in from sample in samples
                 let candidateValue = Convert.ToDouble(candidate.GetValueAtBeat(sample.Beat))
                 select Math.Abs(candidateValue - sample.Value))
        {
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
        normalizedRmse = Math.Sqrt(sumSquaredError / Math.Max(1, samples.Count)) / errorScale;
        return true;
    }

    private SourceProfile AnalyzeSourceProfile(List<SamplePoint> samples)
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
            IsMonotonic(samples.Select(s => s.Progress)));
    }

    private CandidateProfile AnalyzeCandidateProfile(
        Kpc.Event<TPayload> candidate,
        List<SamplePoint> samples)
    {
        var startValue = Convert.ToDouble(candidate.StartValue);
        var endValue = Convert.ToDouble(candidate.EndValue);
        var totalDelta = endValue - startValue;

        var progresses = new List<double>(samples.Count);
        foreach (var sample in samples)
        {
            var value = Convert.ToDouble(candidate.GetValueAtBeat(sample.Beat));
            var progress = Math.Abs(totalDelta) <= NumericEpsilon ? 0d : (value - startValue) / totalDelta;
            progresses.Add(progress);
        }

        return new CandidateProfile(
            GetProgressAtTime(samples, progresses, 0.25d),
            GetProgressAtTime(samples, progresses, 0.50d),
            GetProgressAtTime(samples, progresses, 0.75d),
            HasOvershoot(progresses),
            IsMonotonic(progresses));
    }

    private double GetProgressAtTime(List<SamplePoint> samples, double targetTime)
        => GetProgressAtTime(samples, samples.Select(s => s.Progress).ToList(), targetTime);

    private double GetProgressAtTime(List<SamplePoint> samples, List<double> progresses, double targetTime)
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

    private bool HasOvershoot(IEnumerable<double> progresses)
        => progresses.Any(p => p is < -0.01d or > 1.01d);

    private bool IsMonotonic(IEnumerable<double> progresses)
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

    private EasingPhase DetectPhase(double p25, double p75)
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

    private EasingPhase GetEasingPhase(int easingId)
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

    private Kpc.Event<TPayload> CreateCandidateEvent(Kpc.Event<TPayload> first, Kpc.Event<TPayload> last,
        int easingId)
        => new()
        {
            StartBeat = new Beat((int[])first.StartBeat),
            EndBeat = new Beat((int[])last.EndBeat),
            StartValue = first.StartValue,
            EndValue = last.EndValue,
            Easing = new Kpc.Easing(easingId),
            Font = first.Font
        };

    private Beat LerpBeat(Beat startBeat, Beat endBeat, double t)
        => new((double)startBeat + ((double)endBeat - (double)startBeat) * t);

    private bool CanParticipateInFit(Kpc.Event<TPayload> evt)
        => evt.EndBeat > evt.StartBeat && !evt.IsBezier && evt.Easing == 1;

    private bool CanAppendToFitRun(
        Kpc.Event<TPayload> previousEvent,
        Kpc.Event<TPayload> currentEvent,
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

    private bool AreClose(double left, double right, double tolerance)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), 1d);
        return Math.Abs(left - right) <= tolerance / 100d * scale;
    }

    private int ResolveMaxDegreeOfParallelism(int? maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism is null)
            return Math.Max(1, Environment.ProcessorCount);

        if (maxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism),
                "MaxDegreeOfParallelism must be greater than 0.");

        return maxDegreeOfParallelism.Value;
    }

    private void EnsureSupportedNumericType()
    {
        if (typeof(TPayload) != typeof(int) && typeof(TPayload) != typeof(float) && typeof(TPayload) != typeof(double))
            throw new NotSupportedException("EventListFit only supports int, float, and double types.");
    }

    private readonly record struct SamplePoint(Beat Beat, double Time, double Value, double Progress);

    private readonly record struct SourceProfile(
        double Progress25,
        double Progress50,
        double Progress75,
        EasingPhase Phase,
        bool HasOvershootValue,
        bool IsMonotonicValue);

    private readonly record struct CandidateProfile(
        double Progress25,
        double Progress50,
        double Progress75,
        bool HasOvershootValue,
        bool IsMonotonicValue);

    private readonly record struct FittedSegment(Kpc.Event<TPayload> Event, double Score);

    private readonly record struct PlanChoice(double Cost, int Segments, int Prev, Kpc.Event<TPayload> Event);

    private readonly record struct FitUnit(int Start, int EndExclusive, bool NeedsFit);

    private enum EasingPhase
    {
        Unknown,
        Linear,
        In,
        Out,
        InOut
    }
}