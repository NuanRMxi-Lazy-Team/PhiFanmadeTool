using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;

namespace PhiFanmade.OpenTool.Utils;

public class RePhiEditUtility
{
    public static class CoordinateTransform
    {
        public static float ToPhiEditX(float rePhiEditX)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxX;
            return (rePhiEditX - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }

        public static float ToPhiEditY(float rePhiEditY)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxY;
            return (rePhiEditY - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }
    }

    public static Action<string> OnWarning = s => { };
    
    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static RePhiEdit.JudgeLine FatherUnbind(int targetJudgeLineIndex,List<RePhiEdit.JudgeLine> allJudgeLines)
    {
        throw new NotImplementedException("FatherUnbind is not implemented yet.");
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }
            var fatherLine = allJudgeLinesCopy[judgeLineCopy.Father];
            // 复制一份fatherLine的EventLayers
            var fatherEventLayers = fatherLine.EventLayers.ToList();
            // 层级合并
            fatherEventLayers.RemoveAll(layer => layer == null);
            if (fatherEventLayers.Count <= 1)
            {
                OnWarning.Invoke("FatherUnbind: Father JudgeLine layers count less than or equal to 1, no need to merge.");
                return judgeLineCopy;
            }

            var mergedLayer = new RePhiEdit.EventLayer();
            foreach (var layer in fatherEventLayers)
            {
                if (layer.MoveXEvents.Count > 0)
                    mergedLayer.MoveXEvents = EventMerge(layer.MoveXEvents, mergedLayer.MoveXEvents);
                if (layer.MoveYEvents.Count > 0)
                    mergedLayer.MoveYEvents = EventMerge(layer.MoveYEvents, mergedLayer.MoveYEvents);
                if (layer.RotateEvents.Count > 0 && judgeLineCopy.RotateWithFather)
                    mergedLayer.RotateEvents = EventMerge(layer.RotateEvents, mergedLayer.RotateEvents);
            }
            // 检查judgeLine的EventLayers的Count是否大于等于4，如果大于等于4，则将mergedLayer与最后一个EventLayer合并，否则直接添加mergedLayer
            if (judgeLineCopy.EventLayers.Count >= 4)
            {
                var lastLayer = judgeLineCopy.EventLayers.Last();
                if (lastLayer.MoveXEvents.Count > 0)
                    lastLayer.MoveXEvents = EventMerge(mergedLayer.MoveXEvents, lastLayer.MoveXEvents);
                else
                    lastLayer.MoveXEvents = mergedLayer.MoveXEvents;
                if (lastLayer.MoveYEvents.Count > 0)
                    lastLayer.MoveYEvents = EventMerge(mergedLayer.MoveYEvents, lastLayer.MoveYEvents);
                else
                    lastLayer.MoveYEvents = mergedLayer.MoveYEvents;
                if (lastLayer.RotateEvents.Count > 0 && judgeLineCopy.RotateWithFather)
                    lastLayer.RotateEvents = EventMerge(mergedLayer.RotateEvents, lastLayer.RotateEvents);
                else
                    lastLayer.RotateEvents = mergedLayer.RotateEvents;
            }
            else
            {
                judgeLineCopy.EventLayers.Add(mergedLayer);
            }
            // 解绑father
            judgeLineCopy.Father = -1;
            return judgeLineCopy;

        }
        catch (NullReferenceException)
        {
            OnWarning.Invoke("FatherUnbind: It seems that something is null.");
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            OnWarning.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEventsCopy">源事件列表</param>
    /// <param name="formEventsCopy">要合并进源事件的事件列表</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<RePhiEdit.Event<T>> EventMerge<T>(
        List<RePhiEdit.Event<T>> toEvents, List<RePhiEdit.Event<T>> formEvents)
    {
        var toEventsCopy = toEvents.Select(e => e.Clone()).ToList();
        var formEventsCopy = formEvents.Select(e => e.Clone()).ToList();
        if (toEventsCopy == null || toEventsCopy.Count == 0)
            return formEventsCopy.ToList() ?? new();


        if (formEventsCopy == null || formEventsCopy.Count == 0)
            return toEventsCopy.ToList();
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
        {
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
        }


        // 将formEvents合并进toEvents，先检查是否有重合事件
        var overlapFound = false;
        foreach (var formEvent in formEventsCopy)
        {
            foreach (var toEvent in toEventsCopy)
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
            var newEvents = new List<RePhiEdit.Event<T>>();
            // 获得所有重合区间，比如，formEvents在1~2、4~8拍有事件，toEvents在1~8拍有事件，则重合区间以较长的为准，即1~8拍
            var overlapIntervals = new List<(RePhiEdit.Beat Start, RePhiEdit.Beat End)>();
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
            foreach (var toEvent in toEventsCopy)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    toEvent.StartBeat < interval.End && toEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    newEvents.Add(toEvent);
                }
            }

            // 由于当前数值是两个事件列表的数值相加得到的，所以未重合的formEvents事件需要加上toEvents在该拍的数值
            foreach (var formEvent in formEventsCopy)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    formEvent.StartBeat < interval.End && formEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    // 获得这个事件StartBeat前的第一个toEvent的结束值
                    var previousToEvent = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                    var toEventValue = previousToEvent != null ? previousToEvent.EndValue : (T)default;
                    // 直接修改formEvent的StartValue和EndValue
                    formEvent.StartValue = (dynamic)formEvent.StartValue + (dynamic)toEventValue;
                    formEvent.EndValue = (dynamic)formEvent.EndValue + (dynamic)toEventValue;
                    newEvents.Add(formEvent);
                }
            }


            // 对每个区间内的事件进行切割，两个事件列表都要做切割，切割后的事件长度为0.0625f
            //var cutLength = new RePhiEdit.Beat(0.0625d); // 0.0625拍
            //var cutLength = new RePhiEdit.Beat(0.125f);
            var cutLength = new RePhiEdit.Beat(0.015625d);
            var cutedToEvents = new List<RePhiEdit.Event<T>>();
            var cutedFormEvents = new List<RePhiEdit.Event<T>>();
            foreach (var (start, end) in overlapIntervals)
            {
                // 切割toEvents内的事件
                var toEventsToCut = toEventsCopy.Where(e => e.StartBeat < end && e.EndBeat > start).ToList();
                foreach (var toEvent in toEventsToCut)
                {
                    toEventsCopy.Remove(toEvent);
                    var cutStart = toEvent.StartBeat < start ? start : toEvent.StartBeat;
                    var cutEnd = toEvent.EndBeat > end ? end : toEvent.EndBeat;
                    var currentBeat = cutStart;
                    while (currentBeat < cutEnd)
                    {
                        var segmentEnd = currentBeat + cutLength;
                        if (segmentEnd > cutEnd)
                            segmentEnd = cutEnd;
                        var newEvent = new RePhiEdit.Event<T>
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
                var formEventsToCut = formEventsCopy.Where(e => e.StartBeat < end && e.EndBeat > start).ToList();
                foreach (var formEvent in formEventsToCut)
                {
                    formEventsCopy.Remove(formEvent);
                    var cutStart = formEvent.StartBeat < start ? start : formEvent.StartBeat;
                    var cutEnd = formEvent.EndBeat > end ? end : formEvent.EndBeat;
                    var currentBeat = cutStart;
                    while (currentBeat < cutEnd)
                    {
                        var segmentEnd = currentBeat + cutLength;
                        if (segmentEnd > cutEnd)
                            segmentEnd = cutEnd;
                        var newEvent = new RePhiEdit.Event<T>
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
            var allCutedEvents = new List<RePhiEdit.Event<T>>();
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
                    
                    // 计算合并后的值
                    var toStartValue = toEvent != null ? toEvent.StartValue : toOverlapEventLastEndValue;
                    var formStartValue = formEvent != null ? formEvent.StartValue : formOverlapEventLastEndValue;
                    var startValue = (dynamic)toStartValue + (dynamic)formStartValue;

                    var toEndValue = toEvent != null ? toEvent.EndValue : toOverlapEventLastEndValue;
                    var formEndValue = formEvent != null ? formEvent.EndValue : formOverlapEventLastEndValue;
                    var endValue = (dynamic)toEndValue + (dynamic)formEndValue;
                    
                    var newEvent = new RePhiEdit.Event<T>
                    {
                        StartBeat = currentBeat,
                        EndBeat = nextBeat,
                        StartValue = (dynamic)startValue,
                        EndValue = (dynamic)endValue,
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
            // 最后把newEvents赋值给toEvents
            toEventsCopy = newEvents;

            // 按开始拍排序
            toEventsCopy.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }
        else // 如果没有重合事件,直接合并
        {
            //toEvents.AddRange(formEvents);

            foreach (var formEvent in formEventsCopy)
            {
                var previousToEvent = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                var toEventValue = previousToEvent != null ? previousToEvent.EndValue : default;
                var mergedEvent = new RePhiEdit.Event<T>
                {
                    StartBeat = formEvent.StartBeat,
                    EndBeat = formEvent.EndBeat,
                    StartValue = (dynamic)formEvent.StartValue + (dynamic)toEventValue,
                    EndValue = (dynamic)formEvent.EndValue + (dynamic)toEventValue
                };
                toEventsCopy.Add(mergedEvent);
            }


            // 合并后按开始拍排序
            toEventsCopy.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }

        return toEventsCopy;
    }

    public static RePhiEdit.EventLayer LayerMerge(List<RePhiEdit.EventLayer> layers)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
        {
            OnWarning.Invoke("LayerMerge: layers count less than or equal to 1, no need to merge.");
            return layers.FirstOrDefault() ?? new RePhiEdit.EventLayer();
        }

        var mergedLayer = new RePhiEdit.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents.Count > 0)
                mergedLayer.AlphaEvents = EventMerge(layer.AlphaEvents, mergedLayer.AlphaEvents);
            if (layer.MoveXEvents.Count > 0)
                mergedLayer.MoveXEvents = EventMerge(layer.MoveXEvents, mergedLayer.MoveXEvents);
            if (layer.MoveYEvents.Count > 0)
                mergedLayer.MoveYEvents = EventMerge(layer.MoveYEvents, mergedLayer.MoveYEvents);
            if (layer.RotateEvents.Count > 0)
                mergedLayer.RotateEvents = EventMerge(layer.RotateEvents, mergedLayer.RotateEvents);
            if (layer.SpeedEvents.Count > 0)
                mergedLayer.SpeedEvents = EventMerge(layer.SpeedEvents, mergedLayer.SpeedEvents);
        }

        return mergedLayer;
    }
}