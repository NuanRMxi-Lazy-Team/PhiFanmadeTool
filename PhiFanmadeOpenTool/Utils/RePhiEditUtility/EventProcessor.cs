using static PhiFanmade.Core.RePhiEdit.RePhiEdit;

namespace PhiFanmade.OpenTool.Utils.RePhiEditUtility;

/// <summary>
/// RePhiEdit事件处理器
/// </summary>
internal static class EventProcessor
{
    /// <summary>
    /// 泛型加法辅助方法，避免在 AOT 中使用 dynamic
    /// </summary>
    internal static T AddValues<T>(T a, T b)
    {
        if (typeof(T) == typeof(int))
            return (T)(object)((int)(object)a! + (int)(object)b!);
        if (typeof(T) == typeof(float))
            return (T)(object)((float)(object)a! + (float)(object)b!);
        if (typeof(T) == typeof(double))
            return (T)(object)((double)(object)a! + (double)(object)b!);
        throw new NotSupportedException($"Type {typeof(T)} is not supported for addition");
    }

    /// <summary>
    /// 在指定的拍范围内切割事件列表
    /// </summary>
    /// <param name="events">要切割的事件列表</param>
    /// <param name="startBeat">开始拍</param>
    /// <param name="endBeat">结束拍</param>
    /// <param name="cutLength">切割长度（默认0.015625拍）</param>
    /// <typeparam name="T">事件值类型</typeparam>
    /// <returns>切割后的事件列表</returns>
    internal static List<Event<T>> CutEventsInRange<T>(
        List<Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat? cutLength = null)
    {
        var length = cutLength ?? new Beat(1d / 64d);
        var cutedEvents = new List<Event<T>>();

        // 找到在指定范围内的事件
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd = evt.EndBeat > endBeat ? endBeat : evt.EndBeat;

            // 计算需要切割的段数，避免浮点累加误差
            var totalBeats = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling((double)(totalBeats / length));

            for (int i = 0; i < segmentCount; i++)
            {
                // 使用索引计算位置，而不是累加
                var currentBeat = new Beat(cutStart + (length * i));
                var segmentEnd = new Beat(cutStart + (length * (i + 1)));

                // 最后一段可能需要调整
                if (segmentEnd > cutEnd)
                    segmentEnd = cutEnd;

                var newEvent = new Event<T>
                {
                    StartBeat = currentBeat,
                    EndBeat = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue = evt.GetValueAtBeat(segmentEnd),
                };
                cutedEvents.Add(newEvent);
            }
        }

        return cutedEvents;
    }

    /// <summary>
    /// 压缩事件列表，合并数值变化率相同且相连的线性事件
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">拟合容差，越大拟合精细度越低</param>
    /// <returns>压缩后的事件列表</returns>
    public static List<Event<float>> EventListCompress(List<Event<float>> events,
        double tolerance = 5)
    {
        if (events == null || events.Count == 0)
            return new List<Event<float>>();

        var compressed = new List<Event<float>> { events[0] };

        for (int i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            // 两个事件必须为线性事件，且两个事件数值变化率相同，且结束拍起始拍相连，且结束数值起始数值相等
            if (lastEvent.Easing == 1 && currentEvent.Easing == 1)
            {
                var lastRate = (lastEvent.EndValue - lastEvent.StartValue) /
                               (lastEvent.EndBeat - lastEvent.StartBeat);
                var currentRate = (currentEvent.EndValue - currentEvent.StartValue) /
                                  (currentEvent.EndBeat - currentEvent.StartBeat);

                if (Math.Abs((double)(lastRate - currentRate)) < tolerance &&
                    lastEvent.EndBeat == currentEvent.StartBeat &&
                    Math.Abs((double)(lastEvent.EndValue - currentEvent.StartValue)) < tolerance)
                {
                    // 合并事件
                    lastEvent.EndBeat = currentEvent.EndBeat;
                    lastEvent.EndValue = currentEvent.EndValue;
                    continue;
                }
            }

            // 无法合并时，添加当前事件
            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <param name="precision">切割精细度</param>
    /// <param name="tolerance">数值拟合容差</param>
    /// <typeparam name="T">呃</typeparam>
    /// <returns>已合并的事件列表</returns>
    public static List<Event<T>> EventMerge<T>(
        List<Event<T>> toEvents,
        List<Event<T>> formEvents,
        double precision = 64d,
        double tolerance = 5d)
    {
        // 先检查 null，避免 LINQ "Value cannot be null. (Parameter 'source')" 错误
        if (toEvents == null || toEvents.Count == 0)
        {
            if (formEvents == null || formEvents.Count == 0)
                return new();
            return formEvents.Select(e => e.Clone()).ToList();
        }

        if (formEvents == null || formEvents.Count == 0)
        {
            return toEvents.Select(e => e.Clone()).ToList();
        }

        var toEventsCopy = toEvents.Select(e => e.Clone()).ToList();
        var formEventsCopy = formEvents.Select(e => e.Clone()).ToList();
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
        {
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
        }


        // 将formEvents合并进toEvents，先检查是否有重合事件
        var overlapFound = false;
        foreach (var formEvent in formEventsCopy)
        {
            if (toEventsCopy.Any(toEvent =>
                    formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat))
            {
                overlapFound = true;
            }

            if (overlapFound)
                break;
        }

        if (overlapFound)
        {
            // 获得所有重合区间
            var overlapIntervals = new List<(Beat Start, Beat End)>();
            foreach (var formEvent in formEventsCopy)
            {
                foreach (var toEvent in toEventsCopy)
                {
                    if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                    {
                        var start = formEvent.StartBeat < toEvent.StartBeat ? formEvent.StartBeat : toEvent.StartBeat;
                        var end = formEvent.EndBeat > toEvent.EndBeat ? formEvent.EndBeat : toEvent.EndBeat;
                        // 如果已经存在，不要添加
                        if (overlapIntervals.Any(interval => interval.Start == start && interval.End == end))
                            continue;
                        // 如果与其它overlapInterval有重合，进行扩展
                        if (overlapIntervals.Any(interval =>
                                start < interval.End && end > interval.Start))
                        {
                            for (var i = 0; i < overlapIntervals.Count; i++)
                            {
                                var interval = overlapIntervals[i];
                                if (start < interval.End && end > interval.Start)
                                {
                                    var newStart = start < interval.Start ? start : interval.Start;
                                    var newEnd = end > interval.End ? end : interval.End;
                                    overlapIntervals[i] = (newStart, newEnd);
                                    start = newStart;
                                    end = newEnd;
                                }
                            }
                        }
                        else overlapIntervals.Add((start, end)); // 否则直接添加
                    }
                }
            }

            // 按开始拍排序重合区间
            overlapIntervals.Sort((a, b) =>
                a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));


            // 先把未重合的事件加入newEvents（注意需要加上另一侧最近结束值的偏移）
            var newEvents = (from toEvent in toEventsCopy
                let isInOverlap = overlapIntervals.Any(interval =>
                    toEvent.StartBeat < interval.End && toEvent.EndBeat > interval.Start)
                where !isInOverlap
                let previousFormEvent = formEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat)
                let formOffset = previousFormEvent != null ? previousFormEvent.EndValue : default
                select new Event<T>
                {
                    StartBeat = toEvent.StartBeat,
                    EndBeat = toEvent.EndBeat,
                    StartValue = AddValues(toEvent.StartValue!, formOffset!),
                    EndValue = AddValues(toEvent.EndValue!, formOffset!),
                    BezierPoints = toEvent.BezierPoints,
                    Easing = toEvent.Easing,
                    EasingLeft = toEvent.EasingLeft,
                    EasingRight = toEvent.EasingRight,
                    IsBezier = toEvent.IsBezier,
                }).ToList();

            // 由于当前数值是两个事件列表的数值相加得到的，所以未重合的formEvents事件需要加上toEvents在该拍的数值
            newEvents.AddRange(from formEvent in formEventsCopy
                let isInOverlap = overlapIntervals.Any(interval =>
                    formEvent.StartBeat < interval.End && formEvent.EndBeat > interval.Start)
                where !isInOverlap
                let previousToEvent = toEvents.FindLast(e => e.EndBeat <= formEvent.StartBeat)
                let toEventValue = previousToEvent != null ? previousToEvent.EndValue : (T)default
                select new Event<T>
                {
                    StartBeat = formEvent.StartBeat,
                    EndBeat = formEvent.EndBeat,
                    StartValue = AddValues(formEvent.StartValue, toEventValue!),
                    EndValue = AddValues(formEvent.EndValue, toEventValue!),
                    BezierPoints = formEvent.BezierPoints,
                    Easing = formEvent.Easing,
                    EasingLeft = formEvent.EasingLeft,
                    EasingRight = formEvent.EasingRight,
                    IsBezier = formEvent.IsBezier,
                });


            // 对每个区间内的事件进行切割，两个事件列表都要做切割，切割后的事件长度为0.015625拍
            var cutLength = new Beat(1d / precision);
            var cutedToEvents = new List<Event<T>>();
            var cutedFormEvents = new List<Event<T>>();
            foreach (var (start, end) in overlapIntervals)
            {
                // 使用新方法切割toEvents内的事件
                var cutToInRange = CutEventsInRange(toEventsCopy, start, end, cutLength);
                cutedToEvents.AddRange(cutToInRange);

                // 使用新方法切割formEvents内的事件
                var cutFormInRange = CutEventsInRange(formEventsCopy, start, end, cutLength);
                cutedFormEvents.AddRange(cutFormInRange);

                // 从原列表中移除已切割的事件
                toEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
                formEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
            }

            // 再次合并，现在所有事件长度都一致了，但是要注意，两个事件列表的当前值总和为最终值，无事件的地方使用上一个事件的结束值，没有上一个事件则使用默认值，如果合并不当会导致数值跳变
            var allCutedEvents = new List<Event<T>>();
            T formOverlapEventLastEndValue, toOverlapEventLastEndValue;
            // 以cutLength为采样大小，遍历每一个重合区间
            for (var index = 0; index < overlapIntervals.Count; index++)
            {
                var (start, end) = overlapIntervals[index];
                var currentBeat = start;
                // 在区间开始前，初始化"最近结束值"为区间开始拍之前最近结束的事件值（可能来自区间之外）
                var prevTo = toEventsCopy.FindLast(e => e.EndBeat <= start);
                var prevForm = formEventsCopy.FindLast(e => e.EndBeat <= start);
                formOverlapEventLastEndValue = prevForm != null ? prevForm.EndValue : default;
                toOverlapEventLastEndValue = prevTo != null ? prevTo.EndValue : default;
                while (currentBeat < end)
                {
                    var nextBeat = currentBeat + cutLength;
                    // 以currentBeat为开始拍，nextBeat为结束拍，寻找cutedToEvents和cutedFormEvents内的事件
                    var toEvent =
                        cutedToEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                    var formEvent =
                        cutedFormEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);

                    // 计算合并后的值
                    var toStartValue = toEvent != null ? toEvent.StartValue : toOverlapEventLastEndValue;
                    var formStartValue = formEvent != null ? formEvent.StartValue : formOverlapEventLastEndValue;
                    var startValue = AddValues(toStartValue, formStartValue);

                    var toEndValue = toEvent != null ? toEvent.EndValue : toOverlapEventLastEndValue;
                    var formEndValue = formEvent != null ? formEvent.EndValue : formOverlapEventLastEndValue;
                    var endValue = AddValues(toEndValue, formEndValue);

                    var newEvent = new Event<T>
                    {
                        StartBeat = currentBeat,
                        EndBeat = nextBeat,
                        StartValue = startValue!,
                        EndValue = endValue!,
                    };

                    allCutedEvents.Add(newEvent);

                    // 更新最后的结束值
                    if (toEvent != null) toOverlapEventLastEndValue = toEvent.EndValue;
                    if (formEvent != null) formOverlapEventLastEndValue = formEvent.EndValue;

                    currentBeat = nextBeat;
                }
            }

            // 把切割后的事件加入newEvents
            newEvents.AddRange(allCutedEvents);
            // 如果T为float，则进行压缩
            if (typeof(T) == typeof(float))
                newEvents = EventListCompress(newEvents as List<Event<float>>, tolerance)
                    .Select(e => e as Event<T>).ToList();
            // 最后把newEvents赋值给toEvents
            toEventsCopy = newEvents;

            // 按开始拍排序
            toEventsCopy.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }
        else // 如果没有重合事件,直接合并
        {
            foreach (var formEvent in formEventsCopy)
            {
                var previousToEvent = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                var toEventValue = previousToEvent != null ? previousToEvent.EndValue : default;
                var mergedEvent = new Event<T>
                {
                    StartBeat = formEvent.StartBeat,
                    EndBeat = formEvent.EndBeat,
                    StartValue = AddValues(formEvent.StartValue, toEventValue)!,
                    EndValue = AddValues(formEvent.EndValue, toEventValue)!,
                    BezierPoints = formEvent.BezierPoints,
                    Easing = formEvent.Easing,
                    EasingLeft = formEvent.EasingLeft,
                    EasingRight = formEvent.EasingRight,
                    IsBezier = formEvent.IsBezier,
                };
                toEventsCopy.Add(mergedEvent);
            }


            // 合并后按开始拍排序
            toEventsCopy.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }

        return toEventsCopy;
    }

    /// <summary>
    /// 移除无用事件（起始值和结束值都为默认值的事件）
    /// </summary>
    internal static List<Event<T>>? RemoveUnlessEvent<T>(List<Event<T>>? events)
    {
        // 在确保events不是null的情况下Copy一份，防止对原始列表篡改
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy != null && eventsCopy.Count == 1 &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default(T)) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue, default(T)))
        {
            eventsCopy.RemoveAt(0);
        }

        return eventsCopy;
    }
}