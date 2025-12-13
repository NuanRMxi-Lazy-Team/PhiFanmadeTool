namespace PhiFanmade.OpenTool.Utils;

public class RePhiEdit
{
    public static class CoordinateTransform
    {
        public static float ToPhiEditX(float rePhiEditX)
        {
            var rpeMin = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MaxX;
            return (rePhiEditX - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }

        public static float ToPhiEditY(float rePhiEditY)
        {
            var rpeMin = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MaxY;
            return (rePhiEditY - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }
    }

    public static Action<string> OnWarning = s => { };

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<Core.RePhiEdit.RePhiEdit.Event<T>> EventMerge<T>(
        List<Core.RePhiEdit.RePhiEdit.Event<T>> toEvents, List<Core.RePhiEdit.RePhiEdit.Event<T>> formEvents)
    {
        // 将formEvents合并进toEvents，先检查是否有重合事件
        var overlapFound = false;
        foreach (var formEvent in formEvents)
        {
            foreach (var toEvent in toEvents)
            {
                if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                {
                    OnWarning.Invoke(
                        $"EventMerge: Overlapping events detected between {formEvent.StartBeat}-{formEvent.EndBeat} and {toEvent.StartBeat}-{toEvent.EndBeat}.");
                    overlapFound = true;
                    break;
                }
            }

            if (overlapFound)
                break;
        }

        if (overlapFound)
        {
            var newEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            // 获得所有重合区间，比如，formEvents在1~2、4~8拍有事件，toEvents在1~8拍有事件，则重合区间以较长的为准，即1~8拍
            var overlapIntervals = new List<(Core.RePhiEdit.RePhiEdit.Beat Start, Core.RePhiEdit.RePhiEdit.Beat End)>();
            foreach (var formEvent in formEvents)
            {
                foreach (var toEvent in toEvents)
                {
                    if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                    {
                        var start = formEvent.StartBeat < toEvent.StartBeat ? formEvent.StartBeat : toEvent.StartBeat;
                        var end = formEvent.EndBeat > toEvent.EndBeat ? formEvent.EndBeat : toEvent.EndBeat;
                        // 如果已经存在，不要添加
                        if (overlapIntervals.Any(interval => interval.Start == start && interval.End == end))
                            continue;
                        // 如果与其它overlapInterval有重合，进行合并（扩展）
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

            // 先把未重合的事件加入newEvents
            foreach (var toEvent in toEvents)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    toEvent.StartBeat < interval.End && toEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    newEvents.Add(toEvent);
                }
            }

            // 由于当前数值是两个事件列表的数值相加得到的，所以未重合的formEvents事件需要加上toEvents在该拍的数值
            foreach (var formEvent in formEvents)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    formEvent.StartBeat < interval.End && formEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    // 获得这个事件StartBeat前的第一个toEvent的结束值
                    var previousToEvent = toEvents.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                    var toEventValue = previousToEvent != null ? previousToEvent.EndValue : (T)default;
                    // 直接修改formEvent的StartValue和EndValue
                    formEvent.StartValue = (dynamic)formEvent.StartValue + (dynamic)toEventValue;
                    formEvent.EndValue = (dynamic)formEvent.EndValue + (dynamic)toEventValue;
                    newEvents.Add(formEvent);
                }
            }


            // 对每个区间内的事件进行切割，两个事件列表都要做切割，切割后的事件长度为0.0625f
            //var cutLength = new Core.RePhiEdit.RePhiEdit.Beat(new[] { 0, 31, 500 }); // 0.0625拍
            var cutLength = new Core.RePhiEdit.RePhiEdit.Beat(0.125f);
            var cutedToEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            var cutedFormEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            foreach (var (start, end) in overlapIntervals)
            {
                // 切割toEvents内的事件
                var toEventsToCut = toEvents.Where(e => e.StartBeat < end && e.EndBeat > start).ToList();
                foreach (var toEvent in toEventsToCut)
                {
                    toEvents.Remove(toEvent);
                    var cutStart = toEvent.StartBeat < start ? start : toEvent.StartBeat;
                    var cutEnd = toEvent.EndBeat > end ? end : toEvent.EndBeat;
                    var currentBeat = cutStart;
                    while (currentBeat < cutEnd)
                    {
                        var segmentEnd = currentBeat + cutLength;
                        if (segmentEnd > cutEnd)
                            segmentEnd = cutEnd;
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = segmentEnd,
                            StartValue = toEvent.GetValueAtBeat(currentBeat),
                            EndValue = toEvent.GetValueAtBeat(segmentEnd),
                        };
                        cutedToEvents.Add(newEvent);
                        currentBeat = segmentEnd;
                    }
                }

                // 切割formEvents内的事件
                var formEventsToCut = formEvents.Where(e => e.StartBeat < end && e.EndBeat > start).ToList();
                foreach (var formEvent in formEventsToCut)
                {
                    formEvents.Remove(formEvent);
                    var cutStart = formEvent.StartBeat < start ? start : formEvent.StartBeat;
                    var cutEnd = formEvent.EndBeat > end ? end : formEvent.EndBeat;
                    var currentBeat = cutStart;
                    while (currentBeat < cutEnd)
                    {
                        var segmentEnd = currentBeat + cutLength;
                        if (segmentEnd > cutEnd)
                            segmentEnd = cutEnd;
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = segmentEnd,
                            StartValue = formEvent.GetValueAtBeat(currentBeat),
                            EndValue = formEvent.GetValueAtBeat(segmentEnd),
                        };
                        cutedFormEvents.Add(newEvent);
                        currentBeat = segmentEnd;
                    }
                }
            }

            // 再次合并，现在所有事件长度都一致了，但是要注意，两个事件列表的当前值总和为最终值，无事件的地方使用上一个事件的结束值，没有上一个事件则使用默认值，如果合并不当会导致数值跳变
            var allCutedEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            var formOverlapEventLastEndValue = (T)default;
            var toOverlapEventLastEndValue = (T)default;
            // 以0.125拍为采样大小，遍历每一个重合区间
            for (var index = 0; index < overlapIntervals.Count; index++)
            {
                var (start, end) = overlapIntervals[index];
                var currentBeat = start;
                T defaultValue = default;
                formOverlapEventLastEndValue = default;
                toOverlapEventLastEndValue = default;
                while (currentBeat < end)
                {
                    var nextBeat = currentBeat + cutLength;
                    // 以currentBeat为开始拍，nextBeat为结束拍，寻找cutedToEvents和cutedFormEvents内的事件
                    var toEvent =
                        cutedToEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                    var formEvent =
                        cutedFormEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                    if (toEvent != null && formEvent != null)
                    {
                        // 合并toEvent和formEvent的StartValue和EndValue
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = nextBeat,
                            StartValue = (dynamic)formEvent.StartValue + (dynamic)toEvent.StartValue,
                            EndValue = (dynamic)formEvent.EndValue + (dynamic)toEvent.EndValue,
                        };
                        allCutedEvents.Add(newEvent);
                        formOverlapEventLastEndValue = formEvent.EndValue;
                        toOverlapEventLastEndValue = toEvent.EndValue;
                        currentBeat = nextBeat;
                    }
                    else if (toEvent != null && formEvent == null)
                    {
                        // 只有toEvent
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = nextBeat,
                            StartValue = (dynamic)toEvent.StartValue + (dynamic)formOverlapEventLastEndValue,
                            EndValue = (dynamic)toEvent.EndValue + (dynamic)formOverlapEventLastEndValue,
                        };
                        allCutedEvents.Add(newEvent);
                        toOverlapEventLastEndValue = toEvent.EndValue;
                        currentBeat = nextBeat;
                    }
                    else if (toEvent == null && formEvent != null)
                    {
                        // 只有formEvent
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = nextBeat,
                            StartValue = (dynamic)formEvent.StartValue + (dynamic)toOverlapEventLastEndValue,
                            EndValue = (dynamic)formEvent.EndValue + (dynamic)toOverlapEventLastEndValue,
                        };
                        allCutedEvents.Add(newEvent);
                        formOverlapEventLastEndValue = formEvent.EndValue;
                        currentBeat = nextBeat;
                    }
                    else
                    {
                        // 都没有，直接以toOverlapEventLastEndValue和formOverlapEventLastEndValue为值，StartValue和EndValue相等
                        var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                        {
                            StartBeat = currentBeat,
                            EndBeat = nextBeat,
                            StartValue = (dynamic)toOverlapEventLastEndValue + (dynamic)formOverlapEventLastEndValue,
                            EndValue = (dynamic)toOverlapEventLastEndValue + (dynamic)formOverlapEventLastEndValue,
                        };
                        allCutedEvents.Add(newEvent);
                        currentBeat = nextBeat;
                    }
                }
            }

            // 把切割后的事件加入newEvents
            newEvents.AddRange(allCutedEvents);
            // 最后把newEvents赋值给toEvents
            toEvents = newEvents;

            // 按开始拍排序
            toEvents.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }
        else // 如果没有重合事件,直接合并
        {
            toEvents.AddRange(formEvents);
            // 合并后按开始拍排序
            toEvents.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }

        return toEvents;
    }

    public static Core.RePhiEdit.RePhiEdit.EventLayer LayerMerge(List<Core.RePhiEdit.RePhiEdit.EventLayer> layers)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
        {
            OnWarning.Invoke("LayerMerge: layers count less than or equal to 1, no need to merge.");
            return layers.FirstOrDefault() ?? new Core.RePhiEdit.RePhiEdit.EventLayer();
        }

        var mergedLayer = new Core.RePhiEdit.RePhiEdit.EventLayer();
        foreach (var layer in layers)
        {
            mergedLayer.AlphaEvents = EventMerge(mergedLayer.AlphaEvents, layer.AlphaEvents);
            mergedLayer.MoveXEvents = EventMerge(mergedLayer.MoveXEvents, layer.MoveXEvents);
            mergedLayer.MoveYEvents = EventMerge(mergedLayer.MoveYEvents, layer.MoveYEvents);
            mergedLayer.RotateEvents = EventMerge(mergedLayer.RotateEvents, layer.RotateEvents);
            mergedLayer.SpeedEvents = EventMerge(mergedLayer.SpeedEvents, layer.SpeedEvents);
        }

        return mergedLayer;
    }
}