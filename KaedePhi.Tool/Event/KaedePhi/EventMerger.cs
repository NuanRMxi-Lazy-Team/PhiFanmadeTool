using KaedePhi.Core.Common;
using KaedePhi.Tool.KaedePhi.Events.Internal;

namespace KaedePhi.Tool.Event.KaedePhi;

public class EventMerger<TPayload> : IEventMerger<Kpc.Event<TPayload>>
{
    #region 入口

    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventListMerge(
        List<Kpc.Event<TPayload>>? toEvents,
        List<Kpc.Event<TPayload>>? fromEvents,
        double precision)
    {
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn)) return earlyReturn;
        if (toEvents == null || fromEvents == null) return [];
        EnsureSupportedNumericType();

        var toEventsCopy = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);

        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapFixedSampling(
            toEvents, toEventsCopy, fromEventsCopy, precision);
    }

    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventMergePlus(
        List<Kpc.Event<TPayload>>? toEvents,
        List<Kpc.Event<TPayload>>? fromEvents,
        double precision,
        double tolerance)
    {
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn)) return earlyReturn;
        if (toEvents == null || fromEvents == null) return [];

        EnsureSupportedNumericType();

        var toEventsCopy = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);
        SortByStartBeat(toEventsCopy);
        SortByStartBeat(fromEventsCopy);

        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapAdaptiveSampling(
            toEvents, toEventsCopy, fromEventsCopy, precision, tolerance);
    }

    #endregion

    // 快速返回

    /// <summary>
    /// 在任一输入列表为空时直接给出合并结果，避免进入完整合并流程。
    /// </summary>
    /// <param name="toEvents">目标事件列表。</param>
    /// <param name="fromEvents">来源事件列表。</param>
    /// <param name="result">提前返回时的结果列表。</param>
    /// <returns>若命中提前返回条件则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    private static bool TryGetMergeEarlyReturn(
        List<Kpc.Event<TPayload>>? toEvents,
        List<Kpc.Event<TPayload>>? fromEvents,
        out List<Kpc.Event<TPayload>> result)
    {
        if (toEvents == null || toEvents.Count == 0)
        {
            result = fromEvents == null || fromEvents.Count == 0
                ? []
                : fromEvents.Select(e => e.Clone()).ToList();
            return true;
        }

        if (fromEvents == null || fromEvents.Count == 0)
        {
            result = toEvents.Select(e => e.Clone()).ToList();
            return true;
        }

        result = [];
        return false;
    }

    #region 共用工具
    /// <summary>
    /// 校验事件值类型是否为合并器支持的数值类型。
    /// </summary>
    private static void EnsureSupportedNumericType()
    {
        if (typeof(TPayload) != typeof(int) && typeof(TPayload) != typeof(float) && typeof(TPayload) != typeof(double))
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
    }

    /// <summary>
    /// 深拷贝事件列表，避免修改调用方数据。
    /// </summary>
    /// <param name="events">待拷贝的事件列表。</param>
    /// <returns>拷贝后的新列表。</returns>
    private static List<Kpc.Event<TPayload>> CloneEventList(List<Kpc.Event<TPayload>> events)
        => events.Select(e => e.Clone()).ToList();

    /// <summary>
    /// 按开始拍排序事件列表。
    /// </summary>
    /// <param name="events">待排序的事件列表。</param>
    private static void SortByStartBeat(List<Kpc.Event<TPayload>> events)
        => events.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));

    /// <summary>
    /// 判断两个事件列表是否存在时间重叠。
    /// </summary>
    /// <param name="toEvents">目标事件列表。</param>
    /// <param name="fromEvents">来源事件列表。</param>
    /// <returns>存在任意重叠区间时返回 <see langword="true"/>。</returns>
    private static bool HasOverlap(List<Kpc.Event<TPayload>> toEvents, List<Kpc.Event<TPayload>> fromEvents)
        => fromEvents.Any(fe =>
            toEvents.Any(te => fe.StartBeat < te.EndBeat && fe.EndBeat > te.StartBeat));

    #endregion

    #region 非重叠路径

    /// <summary>
    /// 合并无重叠的两组事件，按前序事件终值补偿偏移。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <returns>合并后的事件列表。</returns>
    private static List<Kpc.Event<TPayload>> MergeWithoutOverlap(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy)
    {
        var newEvents = (from toEvent in toEventsCopy
            let prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat)
            let formOffset = prevForm is null ? default : prevForm.EndValue
            select new Kpc.Event<TPayload>
            {
                StartBeat = toEvent.StartBeat,
                EndBeat = toEvent.EndBeat,
                StartValue = (dynamic?)toEvent.StartValue + (dynamic?)formOffset,
                EndValue = (dynamic?)toEvent.EndValue + (dynamic?)formOffset,
                BezierPoints = toEvent.BezierPoints,
                Easing = toEvent.Easing,
                EasingLeft = toEvent.EasingLeft,
                EasingRight = toEvent.EasingRight,
                IsBezier = toEvent.IsBezier,
            }).ToList();

        newEvents.AddRange(from formEvent in fromEventsCopy
            let prevTo = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo.EndValue ?? default
            select new Kpc.Event<TPayload>
            {
                StartBeat = formEvent.StartBeat,
                EndBeat = formEvent.EndBeat,
                StartValue = (dynamic?)formEvent.StartValue + (dynamic?)toEventValue,
                EndValue = (dynamic?)formEvent.EndValue + (dynamic?)toEventValue,
                BezierPoints = formEvent.BezierPoints,
                Easing = formEvent.Easing,
                EasingLeft = formEvent.EasingLeft,
                EasingRight = formEvent.EasingRight,
                IsBezier = formEvent.IsBezier,
            });

        SortByStartBeat(newEvents);
        return newEvents;
    }

    #endregion

    #region 重叠区间构建

    /// <summary>
    /// 构建两组事件的重叠区间，并将可连接区间归并。
    /// </summary>
    /// <param name="toEvents">目标事件列表。</param>
    /// <param name="fromEvents">来源事件列表。</param>
    /// <returns>按开始拍排序后的重叠区间集合。</returns>
    private static List<(Beat Start, Beat End)> BuildOverlapIntervals(
        List<Kpc.Event<TPayload>> toEvents,
        List<Kpc.Event<TPayload>> fromEvents)
    {
        var overlapIntervals = new List<(Beat Start, Beat End)>();
        foreach (var fe in fromEvents)
        {
            foreach (var te in toEvents)
            {
                if (!TryGetOverlapBounds(fe, te, out var start, out var end)) continue;
                if (overlapIntervals.Any(iv => iv.Start == start && iv.End == end)) continue;
                AddOrMergeOverlapInterval(overlapIntervals, start, end);
            }
        }

        SortIntervals(overlapIntervals);
        return overlapIntervals;
    }

    /// <summary>
    /// 计算两个事件的重叠边界。
    /// </summary>
    /// <param name="fe">来源事件。</param>
    /// <param name="te">目标事件。</param>
    /// <param name="start">重叠起始拍。</param>
    /// <param name="end">重叠结束拍。</param>
    /// <returns>存在重叠时返回 <see langword="true"/>。</returns>
    private static bool TryGetOverlapBounds(Kpc.Event<TPayload> fe, Kpc.Event<TPayload> te, out Beat start, out Beat end)
    {
        if (fe.StartBeat >= te.EndBeat || fe.EndBeat <= te.StartBeat)
        {
            start = new Beat(0d);
            end = new Beat(0d);
            return false;
        }

        // Overlap bounds must be intersection: [max(start), min(end)].
        // Using union here over-expands sampled merge windows and introduces value drift.
        start = fe.StartBeat > te.StartBeat ? fe.StartBeat : te.StartBeat;
        end = fe.EndBeat < te.EndBeat ? fe.EndBeat : te.EndBeat;
        return true;
    }

    /// <summary>
    /// 将新区间加入集合；若与已有区间重叠则进行归并。
    /// </summary>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="start">新区间起始拍。</param>
    /// <param name="end">新区间结束拍。</param>
    private static void AddOrMergeOverlapInterval(
        List<(Beat Start, Beat End)> overlapIntervals, Beat start, Beat end)
    {
        if (!overlapIntervals.Any(iv => start < iv.End && end > iv.Start))
        {
            overlapIntervals.Add((start, end));
            return;
        }

        for (var i = 0; i < overlapIntervals.Count; i++)
        {
            var iv = overlapIntervals[i];
            if (!(start < iv.End && end > iv.Start)) continue;
            var newStart = start < iv.Start ? start : iv.Start;
            var newEnd = end > iv.End ? end : iv.End;
            overlapIntervals[i] = (newStart, newEnd);
            start = newStart;
            end = newEnd;
        }
    }

    /// <summary>
    /// 按起止拍对区间进行稳定排序。
    /// </summary>
    /// <param name="overlapIntervals">待排序区间集合。</param>
    private static void SortIntervals(List<(Beat Start, Beat End)> overlapIntervals)
        => overlapIntervals.Sort((a, b)
            => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));

    #endregion
    
    #region 固定采样路径

    /// <summary>
    /// 通过固定步长切片合并重叠区间，并可按容差压缩结果。
    /// </summary>
    /// <param name="toEventsForOffsetLookup">用于查询前序偏移的目标事件原始序列。</param>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="precision">每拍切片精度。</param>
    /// <returns>合并后的事件列表。</returns>


    private static List<Kpc.Event<TPayload>> MergeWithOverlapFixedSampling(
        List<Kpc.Event<TPayload>> toEventsForOffsetLookup,
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        double precision)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);

        var cutLength = new Beat(1d / precision);
        var (cutTo, cutFrom) =
            CutAndRemoveOverlapEvents(toEventsCopy, fromEventsCopy, overlapIntervals, cutLength);
        newEvents.AddRange(
            MergeCutOverlapSegments(toEventsCopy, fromEventsCopy, cutTo, cutFrom, overlapIntervals, cutLength));

        SortByStartBeat(newEvents);
        return newEvents;
    }

    /// <summary>
    /// 构建重叠区间之外的基础事件，并应用另一轨道的前序偏移。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="toEventsForOffsetLookup">用于查询偏移的目标事件序列。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <returns>重叠区间外的合并事件。</returns>
    private static List<Kpc.Event<TPayload>> BuildBaseEventsOutsideOverlap(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        List<Kpc.Event<TPayload>> toEventsForOffsetLookup,
        List<(Beat Start, Beat End)> overlapIntervals)
    {
        var newEvents = new List<Kpc.Event<TPayload>>();

        foreach (var toEvent in toEventsCopy)
        {
            if (!TouchesAnyOverlap(toEvent))
            {
                // 整条事件在重叠区间外，直接输出（原逻辑）
                var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat);
                var formOffset = prevForm != null ? prevForm.EndValue : default;
                newEvents.Add(new Kpc.Event<TPayload>
                {
                    StartBeat = toEvent.StartBeat,
                    EndBeat = toEvent.EndBeat,
                    StartValue = (dynamic?)toEvent.StartValue + (dynamic?)formOffset,
                    EndValue = (dynamic?)toEvent.EndValue + (dynamic?)formOffset,
                    BezierPoints = toEvent.BezierPoints,
                    Easing = toEvent.Easing,
                    EasingLeft = toEvent.EasingLeft,
                    EasingRight = toEvent.EasingRight,
                    IsBezier = toEvent.IsBezier,
                });
            }
            else
            {
                // 事件与重叠区间有交叉：补出超出重叠区间的片段
                foreach (var (gapStart, gapEnd) in GapsOutsideOverlap(toEvent))
                {
                    var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= gapStart);
                    var formOffset = prevForm != null ? prevForm.EndValue : default;
                    newEvents.Add(new Kpc.Event<TPayload>
                    {
                        StartBeat = gapStart,
                        EndBeat = gapEnd,
                        StartValue = (dynamic?)toEvent.GetValueAtBeat(gapStart) + (dynamic?)formOffset,
                        EndValue = (dynamic?)toEvent.GetValueAtBeat(gapEnd) + (dynamic?)formOffset,
                        BezierPoints = toEvent.BezierPoints,
                        Easing = toEvent.Easing,
                        EasingLeft = toEvent.EasingLeft,
                        EasingRight = toEvent.EasingRight,
                        IsBezier = toEvent.IsBezier,
                    });
                }
            }
        }

        foreach (var formEvent in fromEventsCopy)
        {
            if (!TouchesAnyOverlap(formEvent))
            {
                var prevTo = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                var toEventValue = prevTo != null ? prevTo.EndValue : default;
                newEvents.Add(new Kpc.Event<TPayload>
                {
                    StartBeat = formEvent.StartBeat,
                    EndBeat = formEvent.EndBeat,
                    StartValue = (dynamic?)formEvent.StartValue + (dynamic?)toEventValue,
                    EndValue = (dynamic?)formEvent.EndValue + (dynamic?)toEventValue,
                    BezierPoints = formEvent.BezierPoints,
                    Easing = formEvent.Easing,
                    EasingLeft = formEvent.EasingLeft,
                    EasingRight = formEvent.EasingRight,
                    IsBezier = formEvent.IsBezier,
                });
            }
            else
            {
                foreach (var (gapStart, gapEnd) in GapsOutsideOverlap(formEvent))
                {
                    var prevTo = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= gapStart);
                    var toEventValue = prevTo != null ? prevTo.EndValue : default;
                    newEvents.Add(new Kpc.Event<TPayload>
                    {
                        StartBeat = gapStart,
                        EndBeat = gapEnd,
                        StartValue = (dynamic?)formEvent.GetValueAtBeat(gapStart) + (dynamic?)toEventValue,
                        EndValue = (dynamic?)formEvent.GetValueAtBeat(gapEnd) + (dynamic?)toEventValue,
                        BezierPoints = formEvent.BezierPoints,
                        Easing = formEvent.Easing,
                        EasingLeft = formEvent.EasingLeft,
                        EasingRight = formEvent.EasingRight,
                        IsBezier = formEvent.IsBezier,
                    });
                }
            }
        }

        return newEvents;

        bool TouchesAnyOverlap(Kpc.Event<TPayload> evt)
            => overlapIntervals.Any(iv => evt.StartBeat < iv.End && evt.EndBeat > iv.Start);

        // 返回一个事件在所有重叠区间之外的时间片段列表（即"空隙"）。
        // 例如事件 [223.5, 225.5]、重叠区间 [223.5, 224.0]，空隙为 [(224.0, 225.5)]。
        List<(Beat Start, Beat End)> GapsOutsideOverlap(Kpc.Event<TPayload> evt)
        {
            var gaps = new List<(Beat Start, Beat End)>();
            var cursor = evt.StartBeat;
            // overlapIntervals 已按 Start 排序
            foreach (var iv in overlapIntervals)
            {
                if (iv.Start > cursor && iv.Start < evt.EndBeat)
                    gaps.Add((cursor, iv.Start)); // 空隙在重叠区间左侧
                if (iv.End > cursor)
                    cursor = iv.End; // 推进游标越过当前重叠区间
                if (cursor >= evt.EndBeat) break;
            }

            if (cursor < evt.EndBeat)
                gaps.Add((cursor, evt.EndBeat)); // 末尾剩余空隙
            return gaps;
        }
    }

    /// <summary>
    /// 在重叠区间内切分两组事件，并从原列表移除已覆盖片段。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <returns>目标与来源两组切片事件。</returns>
    private static (List<Kpc.Event<TPayload>> CutTo, List<Kpc.Event<TPayload>> CutFrom) CutAndRemoveOverlapEvents(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength)
    {
        var cutTo = new List<Kpc.Event<TPayload>>();
        var cutFrom = new List<Kpc.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            cutTo.AddRange(EventCutter.CutEventsInRange(toEventsCopy, start, end, cutLength));
            cutFrom.AddRange(EventCutter.CutEventsInRange(fromEventsCopy, start, end, cutLength));
            toEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
            fromEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
        }

        return (cutTo, cutFrom);
    }

    /// <summary>
    /// 逐个重叠区间合并切片事件，并处理缺失片段的延续值。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝（用于查找前序终值）。</param>
    /// <param name="fromEventsCopy">来源事件拷贝（用于查找前序终值）。</param>
    /// <param name="cutTo">目标切片事件。</param>
    /// <param name="cutFrom">来源切片事件。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <returns>重叠区间合并结果。</returns>
    private static List<Kpc.Event<TPayload>> MergeCutOverlapSegments(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        List<Kpc.Event<TPayload>> cutTo,
        List<Kpc.Event<TPayload>> cutFrom,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength)
    {
        var allCutEvents = new List<Kpc.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            var prevTo = toEventsCopy.FindLast(e => e.EndBeat <= start);
            var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= start);
            var toLastEnd = prevTo != null ? prevTo.EndValue : default;
            var formLastEnd = prevForm != null ? prevForm.EndValue : default;
            allCutEvents.AddRange(
                MergeSingleOverlapInterval(cutTo, cutFrom, start, end, cutLength, toLastEnd, formLastEnd));
        }

        return allCutEvents;
    }

    /// <summary>
    /// 合并单个重叠区间内的切片事件。
    /// </summary>
    /// <param name="cutTo">目标切片事件。</param>
    /// <param name="cutFrom">来源切片事件。</param>
    /// <param name="start">区间起始拍。</param>
    /// <param name="end">区间结束拍。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <param name="toLastEndValue">目标轨道进入区间前的终值。</param>
    /// <param name="formLastEndValue">来源轨道进入区间前的终值。</param>
    /// <returns>该区间的合并事件。</returns>
    private static List<Kpc.Event<TPayload>> MergeSingleOverlapInterval(
        List<Kpc.Event<TPayload>> cutTo,
        List<Kpc.Event<TPayload>> cutFrom,
        Beat start, Beat end, Beat cutLength,
        TPayload? toLastEndValue, TPayload? formLastEndValue)
    {
        var merged = new List<Kpc.Event<TPayload>>();
        var currentBeat = start;
        while (currentBeat < end)
        {
            var nextBeat = currentBeat + cutLength;
            if (nextBeat > end) nextBeat = end;
            var toEvent = cutTo.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
            var formEvent = cutFrom.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);

            var toStart = toEvent != null ? toEvent.StartValue : toLastEndValue;
            var formStart = formEvent != null ? formEvent.StartValue : formLastEndValue;
            var toEnd = toEvent != null ? toEvent.EndValue : toLastEndValue;
            var formEnd = formEvent != null ? formEvent.EndValue : formLastEndValue;

            merged.Add(new Kpc.Event<TPayload>
            {
                StartBeat = currentBeat,
                EndBeat = nextBeat,
                StartValue = (dynamic?)toStart + (dynamic?)formStart,
                EndValue = (dynamic?)toEnd + (dynamic?)formEnd,
            });

            if (toEvent != null) toLastEndValue = toEvent.EndValue;
            if (formEvent != null) formLastEndValue = formEvent.EndValue;
            currentBeat = nextBeat;
        }

        return merged;
    }

    #endregion

    #region 自适应采样路径

    /// <summary>
    /// 通过自适应采样合并重叠区间，减少冗余切片。
    /// </summary>
    /// <param name="toEventsForOffsetLookup">用于查询偏移的目标事件序列。</param>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="precision">基础采样精度。</param>
    /// <param name="tolerance">分段误差容差。</param>
    /// <returns>合并后的事件列表。</returns>
    private static List<Kpc.Event<TPayload>> MergeWithOverlapAdaptiveSampling(
        List<Kpc.Event<TPayload>> toEventsForOffsetLookup,
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        double precision,
        double tolerance)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);

        newEvents.AddRange(
            MergeAdaptiveIntervals(toEventsCopy, fromEventsCopy, overlapIntervals, precision, tolerance));

        SortByStartBeat(newEvents);
        return newEvents;
    }

    /// <summary>
    /// 逐个重叠区间执行自适应合并。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="precision">基础采样精度。</param>
    /// <param name="tolerance">分段误差容差。</param>
    /// <returns>所有重叠区间的合并结果。</returns>
    private static List<Kpc.Event<TPayload>> MergeAdaptiveIntervals(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        double precision,
        double tolerance)
    {
        var cutLength = new Beat(1d / precision);
        var result = new List<Kpc.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            result.AddRange(
                MergeAdaptiveSingleInterval(toEventsCopy, fromEventsCopy, start, end, cutLength, tolerance));
        }

        return result;
    }

    /// <summary>
    /// 在单个区间内按误差阈值动态分段，生成近似线性片段。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="start">区间起始拍。</param>
    /// <param name="end">区间结束拍。</param>
    /// <param name="cutLength">基础步长。</param>
    /// <param name="tolerance">误差容差百分比。</param>
    /// <returns>单区间合并后的事件。</returns>
    private static List<Kpc.Event<TPayload>> MergeAdaptiveSingleInterval(
        List<Kpc.Event<TPayload>> toEventsCopy,
        List<Kpc.Event<TPayload>> fromEventsCopy,
        Beat start, Beat end, Beat cutLength, double tolerance)
    {
        var result = new List<Kpc.Event<TPayload>>();
        var currentBeat = start;

        var lastToValue = GetPreviousEndValue(toEventsCopy, start);
        var lastFormValue = GetPreviousEndValue(fromEventsCopy, start);

        var toEventAtCurrent = GetActiveEventAtBeat(toEventsCopy, currentBeat);
        var formEventAtCurrent = GetActiveEventAtBeat(fromEventsCopy, currentBeat);
        var toValAtCurrent = toEventAtCurrent != null ? toEventAtCurrent.GetValueAtBeat(currentBeat) : lastToValue;
        var formValAtCurrent =
            formEventAtCurrent != null ? formEventAtCurrent.GetValueAtBeat(currentBeat) : lastFormValue;

        var segmentStart = start;
        var segmentStartToValue = toValAtCurrent;
        var segmentStartFormValue = formValAtCurrent;
        var segmentStartSum = AddValues(segmentStartToValue, segmentStartFormValue);

        while (currentBeat < end)
        {
            var nextBeat = currentBeat + cutLength;
            if (nextBeat > end) nextBeat = end;

            var toEventAtNext = GetActiveEventAtBeat(toEventsCopy, nextBeat);
            var formEventAtNext = GetActiveEventAtBeat(fromEventsCopy, nextBeat);
            var crossEvent = !ReferenceEquals(toEventAtNext, toEventAtCurrent) ||
                             !ReferenceEquals(formEventAtNext, formEventAtCurrent);

            if (crossEvent && currentBeat > segmentStart)
            {
                AddSegmentEvent(result, segmentStart, currentBeat,
                    segmentStartToValue, segmentStartFormValue, toValAtCurrent, formValAtCurrent);
                segmentStart = currentBeat;
                segmentStartToValue = toValAtCurrent;
                segmentStartFormValue = formValAtCurrent;
                segmentStartSum = AddValues(toValAtCurrent, formValAtCurrent);
            }

            var (toValueAtNext, toValUpdate) =
                GetNextBeatValues(toEventsCopy, toEventAtCurrent, toEventAtNext, nextBeat);
            var (formValueAtNext, formValUpdate) =
                GetNextBeatValues(fromEventsCopy, formEventAtCurrent, formEventAtNext, nextBeat);

            var sumAtNext = AddValues(toValueAtNext, formValueAtNext);
            var sumAtEnd = AddValues(GetValueAtBeatOrPreviousEnd(toEventsCopy, end),
                GetValueAtBeatOrPreviousEnd(fromEventsCopy, end));

            if (crossEvent || ShouldSplitAdaptiveSegment(
                    segmentStart, nextBeat, end, segmentStartSum, sumAtNext, sumAtEnd, tolerance))
            {
                AddSegmentEvent(result, segmentStart, nextBeat,
                    segmentStartToValue, segmentStartFormValue, toValueAtNext, formValueAtNext);
                segmentStart = nextBeat;
                segmentStartToValue = toValUpdate;
                segmentStartFormValue = formValUpdate;
                segmentStartSum = AddValues(toValUpdate, formValUpdate);
            }

            toEventAtCurrent = toEventAtNext;
            formEventAtCurrent = formEventAtNext;
            toValAtCurrent = toValUpdate;
            formValAtCurrent = formValUpdate;
            currentBeat = nextBeat;
        }

        return result;
    }

    /// <summary>
    /// 获取指定拍点处处于激活状态且起始拍最晚的事件。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="beat">查询拍点。</param>
    /// <returns>命中的活动事件；若不存在则为 <see langword="null"/>。</returns>
    private static Kpc.Event<TPayload>? GetActiveEventAtBeat(List<Kpc.Event<TPayload>> events, Beat beat)
        => events.Where(e => e.StartBeat <= beat && e.EndBeat >= beat).MaxBy(e => e.StartBeat);

    /// <summary>
    /// 获取指定拍点之前最近事件的终值。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="beat">查询拍点。</param>
    /// <returns>最近前序终值；不存在时返回默认值。</returns>
    private static TPayload? GetPreviousEndValue(List<Kpc.Event<TPayload>> events, Beat beat)
    {
        var prev = events.FindLast(e => e.EndBeat <= beat);
        return prev != null ? prev.EndValue : default;
    }

    /// <summary>
    /// 获取指定拍点的事件值；若无活动事件则回退到前序终值。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="beat">查询拍点。</param>
    /// <returns>该拍点可用的数值。</returns>
    private static TPayload? GetValueAtBeatOrPreviousEnd(List<Kpc.Event<TPayload>> events, Beat beat)
    {
        var active = GetActiveEventAtBeat(events, beat);
        return active != null ? active.GetValueAtBeat(beat) : GetPreviousEndValue(events, beat);
    }

    /// <summary>
    /// 计算下一拍点的出站值与入站值，用于事件切换边界处理。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="eventAtCurrent">当前拍点活动事件。</param>
    /// <param name="eventAtNext">下一拍点活动事件。</param>
    /// <param name="nextBeat">下一拍点。</param>
    /// <returns>
    /// 一个元组：<c>Outgoing</c> 表示当前事件延续到下一拍的值，<c>Incoming</c> 表示下一拍活动事件值。
    /// </returns>
    private static (TPayload? Outgoing, TPayload? Incoming) GetNextBeatValues(
        List<Kpc.Event<TPayload>> events, Kpc.Event<TPayload>? eventAtCurrent, Kpc.Event<TPayload>? eventAtNext,
        Beat nextBeat)
    {
        var prevEnd = GetPreviousEndValue(events, nextBeat);
        var outgoing = (eventAtCurrent != null && eventAtCurrent.EndBeat >= nextBeat)
            ? eventAtCurrent.GetValueAtBeat(nextBeat)
            : prevEnd;
        var incoming = eventAtNext != null
            ? eventAtNext.GetValueAtBeat(nextBeat)
            : prevEnd;
        return (outgoing, incoming);
    }

    /// <summary>
    /// 使用归一化 (时间, 值) 空间中的欧几里得垂直距离判断当前自适应分段是否需要切分。
    /// <para>
    /// 将时间归一化到 [0, 1]、值归一化到以最大绝对值为比例尺的无量纲空间，
    /// 计算测试点到理想线段的垂直距离（不混用量纲），避免原公式 sqrt(dx²+dy²)−dx 
    /// 因时间单位（拍）与值单位不同而导致的失真。
    /// </para>
    /// </summary>
    /// <param name="segmentStart">分段起始拍。</param>
    /// <param name="nextBeat">待评估拍点。</param>
    /// <param name="intervalEnd">区间结束拍。</param>
    /// <param name="segmentStartSum">分段起点和轨道值。</param>
    /// <param name="sumAtNext">下一拍点和轨道值。</param>
    /// <param name="sumAtEnd">区间终点和轨道值。</param>
    /// <param name="tolerance">允许误差百分比。</param>
    /// <returns>垂直距离超限或到达区间末尾时返回 <see langword="true"/>。</returns>
    private static bool ShouldSplitAdaptiveSegment(
        Beat segmentStart, Beat nextBeat, Beat intervalEnd,
        TPayload? segmentStartSum, TPayload? sumAtNext, TPayload? sumAtEnd, double tolerance)
    {
        if (nextBeat >= intervalEnd) return true;
        if (nextBeat <= segmentStart) return false;

        var dtTotal = (double)(intervalEnd - segmentStart);
        var dtLocal = (double)(nextBeat - segmentStart);
        if (dtTotal <= 1e-12 || dtLocal <= 1e-12) return false;

        var p = Math.Clamp(dtLocal / dtTotal, 0.0, 1.0);

        var startNum = ToDouble(segmentStartSum);
        var nextNum = ToDouble(sumAtNext);
        var endNum = ToDouble(sumAtEnd);

        // 归一化比例尺：避免量纲混用
        var scale = Math.Max(Math.Max(Math.Abs(startNum), Math.Abs(endNum)), 1e-9);

        // 归一化 (时间, 值) 空间中的垂直距离：
        //   A'=(0, 0), C'=(1, dvNorm), 测试点 B'=(p, byNorm)
        //   d = |byNorm − dvNorm·p| / sqrt(1 + dvNorm²)
        // 当 dvNorm≈0（水平段）退化为纯值域偏差，当 dvNorm 很大（陡峭段）退化为时间偏差，
        // 两者通过欧几里得范数自然融合，无需手动加权。
        var dvNorm = (endNum - startNum) / scale;
        var byNorm = (nextNum - startNum) / scale;
        var det = byNorm - dvNorm * p;
        var len = Math.Sqrt(1.0 + dvNorm * dvNorm);
        var normalizedDist = Math.Abs(det) / len;

        return normalizedDist > Math.Max(0d, tolerance) / 100.0;
    }

    /// <summary>
    /// 将动态数值安全转换为 <see cref="double"/>。
    /// </summary>
    /// <param name="value">待转换数值。</param>
    /// <returns>转换后的双精度值。</returns>
    private static double ToDouble(dynamic? value)
    {
        if (value == null)
            throw new InvalidOperationException("Unexpected null numeric value.");
        return (double)value;
    }

    /// <summary>
    /// 对两个可空数值执行动态加法。
    /// </summary>
    /// <typeparam name="TPayload">事件值类型。</typeparam>
    /// <param name="left">左值。</param>
    /// <param name="right">右值。</param>
    /// <returns>加法结果。</returns>
    private static TPayload AddValues(TPayload? left, TPayload? right)
        => (dynamic?)left + (dynamic?)right;

    /// <summary>
    /// 向目标列表追加一个由两轨道叠加得到的分段事件。
    /// </summary>
    /// <param name="target">目标事件列表。</param>
    /// <param name="startBeat">分段起始拍。</param>
    /// <param name="endBeat">分段结束拍。</param>
    /// <param name="startToValue">目标轨道起点值。</param>
    /// <param name="startFormValue">来源轨道起点值。</param>
    /// <param name="endToValue">目标轨道终点值。</param>
    /// <param name="endFormValue">来源轨道终点值。</param>
    private static void AddSegmentEvent(
        List<Kpc.Event<TPayload>> target, Beat startBeat, Beat endBeat,
        TPayload? startToValue, TPayload? startFormValue, TPayload? endToValue, TPayload? endFormValue)
    {
        target.Add(new Kpc.Event<TPayload>
        {
            StartBeat = startBeat,
            EndBeat = endBeat,
            StartValue = AddValues(startToValue, startFormValue),
            EndValue = AddValues(endToValue, endFormValue),
        });
    }

    #endregion
}