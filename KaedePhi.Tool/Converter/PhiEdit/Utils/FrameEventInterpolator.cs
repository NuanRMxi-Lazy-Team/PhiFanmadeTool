using KaedePhi.Core.Common;
using KaedePhi.Tool.KaedePhi.Events;
using KpcEasing = KaedePhi.Core.KaedePhi.Easing;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE Frame/Event 模型到 KPC Event 模型的插值转换器。
/// </summary>
public static class FrameEventInterpolator
{
    private const double TrailingBeatPadding = 1d / 64d;
    private const double FrameEditableSliceBeat = 0.0125d;
    private const double BeatComparisonEpsilon = 1e-6d;

    /// <summary>
    /// 计算单条判定线的时间范围上界，并额外补齐一个微小区间。
    /// </summary>
    public static double GetJudgeLineHorizonBeat(Pe.JudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.EndBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.StartBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.SpeedFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.MoveFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.RotateFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.AlphaFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.MoveEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.RotateEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.AlphaEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        return maxBeat + TrailingBeatPadding;
    }

    /// <summary>
    /// 构建 Move 轴（X 或 Y）事件列表。
    /// </summary>
    public static List<Kpc.Event<double>>? BuildMoveAxisEvents(
        List<Pe.MoveFrame>? frames,
        List<Pe.MoveEvent>? events,
        double horizonBeat,
        Func<(float X, float Y), float> selector,
        Func<float, double> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Kpc.Event<double>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindMoveFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsMoveEventStartBeat(orderedEvents, startBeat))
            {
                var convertedValue = valueTransformer(selector((frameAtBoundary.XValue, frameAtBoundary.YValue)));
                result.Add(CreateConstantEvent(startBeat, endBeat, convertedValue));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveMoveEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveMoveEventStartValue(activeEvent, orderedFrames, orderedEventsByEnd);
                result.Add(new Kpc.Event<double>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = EasingConverter.ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue =
                        valueTransformer(InterpolateMoveValue(activeEvent, startBeat, eventStartSource, selector)),
                    EndValue = valueTransformer(InterpolateMoveValue(activeEvent, endBeat, eventStartSource,
                        selector))
                });
            }
        }

        return result.Count == 0 ? null : KpcEventTools.EventListCompressSqrt(result, 0d);
    }

    /// <summary>
    /// 构建标量通道（旋转、透明度、速度）事件列表。
    /// </summary>
    public static List<Kpc.Event<T>>? BuildScalarEvents<T>(
        List<Pe.Frame>? frames,
        List<Pe.Event>? events,
        double horizonBeat,
        Func<float, T> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Kpc.Event<T>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindScalarFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsScalarEventStartBeat(orderedEvents, startBeat))
            {
                result.Add(CreateConstantEvent(startBeat, endBeat, valueTransformer(frameAtBoundary.Value)));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveScalarEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveScalarEventStartValue(activeEvent, orderedFrames, orderedEventsByEnd);
                result.Add(new Kpc.Event<T>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = EasingConverter.ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue = valueTransformer(InterpolateScalarValue(activeEvent, startBeat, eventStartSource)),
                    EndValue = valueTransformer(InterpolateScalarValue(activeEvent, endBeat, eventStartSource))
                });
            }
        }

        return result.Count == 0 ? null : result;
    }

    #region Helpers

    private static double GetMaxBeat(IEnumerable<double>? beats) => beats?.DefaultIfEmpty(0d).Max() ?? 0d;

    private static double GetMidBeat(double startBeat, double endBeat) => startBeat + (endBeat - startBeat) / 2d;

    private static bool IsSameBeat(double leftBeat, double rightBeat)
        => Math.Abs(leftBeat - rightBeat) <= BeatComparisonEpsilon;

    private static List<double> BuildBoundaries(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        double horizonBeat)
    {
        var boundaries = new SortedSet<double>();
        foreach (var beat in frameBoundaries) boundaries.Add(beat);
        foreach (var beat in eventBoundaries) boundaries.Add(beat);
        if (boundaries.Count == 0) return [];

        boundaries.Add(Math.Max(horizonBeat, boundaries.Max + TrailingBeatPadding));
        return boundaries.ToList();
    }

    private static List<double> BuildBoundariesWithFrameSlices(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        IEnumerable<double> eventStartBoundaries,
        double horizonBeat)
    {
        var frameList = frameBoundaries.ToList();
        var eventStartList = eventStartBoundaries.OrderBy(beat => beat).ToList();
        var boundaries = BuildBoundaries(frameList, eventBoundaries, horizonBeat);
        if (boundaries.Count == 0) return boundaries;

        var expandedBoundaries = new SortedSet<double>(boundaries);
        foreach (var frameBeat in frameList)
        {
            if (ContainsBeat(eventStartList, frameBeat)) continue;
            expandedBoundaries.Add(frameBeat + FrameEditableSliceBeat);
        }

        return expandedBoundaries.ToList();
    }

    private static int FindLastIndexAtOrBeforeBeat<T>(List<T> items, double beat, Func<T, double> beatSelector)
    {
        var lo = 0;
        var hi = items.Count - 1;
        var result = -1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var midBeat = beatSelector(items[mid]);
            if (midBeat <= beat + BeatComparisonEpsilon)
            {
                result = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }

    private static bool ContainsBeat(List<double> sortedBeats, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(sortedBeats, beat, value => value);
        return idx >= 0 && IsSameBeat(sortedBeats[idx], beat);
    }

    private static float GetEventBoundary(float eventStartBeat, float eventEndBeat, double beat)
    {
        var duration = eventEndBeat - eventStartBeat;
        if (Math.Abs(duration) < 1e-6f) return 1f;
        return (float)((beat - eventStartBeat) / duration);
    }

    private static Kpc.Event<T> CreateConstantEvent<T>(double startBeat, double endBeat, T value) => new()
    {
        StartBeat = new Beat(startBeat),
        EndBeat = new Beat(endBeat),
        Easing = new KpcEasing(1),
        EasingLeft = 0f,
        EasingRight = 1f,
        StartValue = value,
        EndValue = value
    };

    private static Pe.MoveEvent? FindActiveMoveEvent(List<Pe.MoveEvent> events, double beat)
    {
        var lo = 0;
        var hi = events.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (events[mid].StartBeat <= beat)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        if (hi >= 0 && events[hi].EndBeat >= beat)
            return events[hi];
        return null;
    }

    private static Pe.Event? FindActiveScalarEvent(List<Pe.Event> events, double beat)
    {
        var lo = 0;
        var hi = events.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (events[mid].StartBeat <= beat)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        if (hi >= 0 && events[hi].EndBeat >= beat)
            return events[hi];
        return null;
    }

    private static (float X, float Y) ResolveMoveValueAfterBoundary(
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events,
        double boundaryBeat)
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(frames, boundaryBeat, frame => frame.Beat);
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(events, boundaryBeat, ev => ev.EndBeat);

        var previousFrame = previousFrameIndex >= 0 ? frames[previousFrameIndex] : null;
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return (previousEvent.EndXValue, previousEvent.EndYValue);

        if (previousFrame != null)
            return (previousFrame.XValue, previousFrame.YValue);

        return (0f, 0f);
    }

    private static float ResolveScalarValueAfterBoundary(
        List<Pe.Frame> frames,
        List<Pe.Event> events,
        double boundaryBeat)
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(frames, boundaryBeat, frame => frame.Beat);
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(events, boundaryBeat, ev => ev.EndBeat);

        var previousFrame = previousFrameIndex >= 0 ? frames[previousFrameIndex] : null;
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return previousEvent.EndValue;

        if (previousFrame != null)
            return previousFrame.Value;

        return 0f;
    }

    private static (float X, float Y) ResolveMoveEventStartValue(
        Pe.MoveEvent ev,
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events)
    {
        var frameAtStart = FindMoveFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return (frameAtStart.XValue, frameAtStart.YValue);

        return ResolveMoveValueAfterBoundary(frames, events, ev.StartBeat);
    }

    private static float ResolveScalarEventStartValue(Pe.Event ev, List<Pe.Frame> frames, List<Pe.Event> events)
    {
        var frameAtStart = FindScalarFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return frameAtStart.Value;

        return ResolveScalarValueAfterBoundary(frames, events, ev.StartBeat);
    }

    private static bool IsMoveEventStartBeat(List<Pe.MoveEvent> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    private static bool IsScalarEventStartBeat(List<Pe.Event> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    private static Pe.MoveFrame? FindMoveFrameAtBeat(List<Pe.MoveFrame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        return idx >= 0 && IsSameBeat(frames[idx].Beat, beat) ? frames[idx] : null;
    }

    private static Pe.Frame? FindScalarFrameAtBeat(List<Pe.Frame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        return idx >= 0 && IsSameBeat(frames[idx].Beat, beat) ? frames[idx] : null;
    }

    private static float InterpolateMoveValue(
        Pe.MoveEvent ev,
        double beat,
        (float X, float Y) intervalStartSource,
        Func<(float X, float Y), float> selector)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return selector((ev.EndXValue, ev.EndYValue));

        return selector(ev.GetValueAtBeat((float)beat, intervalStartSource.X, intervalStartSource.Y));
    }

    private static float InterpolateScalarValue(Pe.Event ev, double beat, float intervalStartSource)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return ev.EndValue;

        return ev.GetValueAtBeat((float)beat, intervalStartSource);
    }

    #endregion
}
