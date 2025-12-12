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
            // 先合并没有重合的事件,并把重合的事件从原始列表中移除,添加到新的列表中
            var overlapToEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            var overlapFormEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            var nonOverlapFormEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();

            // 检查每个formEvent是否与toEvents中的任何事件重合
            foreach (var formEvent in formEvents)
            {
                var hasOverlap = false;
                foreach (var toEvent in toEvents)
                {
                    if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                    {
                        hasOverlap = true;
                        // 将重合的toEvent添加到overlapToEvents(避免重复)
                        if (!overlapToEvents.Contains(toEvent))
                        {
                            overlapToEvents.Add(toEvent);
                        }
                    }
                }

                if (hasOverlap)
                {
                    overlapFormEvents.Add(formEvent);
                }
                else
                {
                    nonOverlapFormEvents.Add(formEvent);
                }
            }

            // 从toEvents中移除重合的事件

            foreach (var overlapEvent in overlapToEvents)
            {
                toEvents.Remove(overlapEvent);
            }


            // 合并没有重合的formEvents到toEvents
            toEvents.AddRange(nonOverlapFormEvents);

            // 从overlapToEvents和overlapFormEvents中取StartBeat最小和EndBeat最大的事件作为采样起点和采样终点
            var allOverlapEvents = overlapToEvents.Concat(overlapFormEvents).ToList();
            var sampleStartBeat = allOverlapEvents[0].StartBeat;
            var sampleEndBeat = allOverlapEvents[0].EndBeat;


            foreach (var e in allOverlapEvents)
            {
                if (e.StartBeat < sampleStartBeat)
                    sampleStartBeat = e.StartBeat;
                if (e.EndBeat > sampleEndBeat)
                    sampleEndBeat = e.EndBeat;
            }

            // 每次采样间隔为0.0625拍
            var sampleInterval = new Core.RePhiEdit.RePhiEdit.Beat(0.0625f);
            var currentBeat = sampleStartBeat;
            // 先别急！如果allOverlapEvents中有事件的长度比sampleInterval还小怎么办？那就以最小事件长度为采样间隔
            var minEventLength = allOverlapEvents.Min(e => (e.EndBeat - e.StartBeat));
            if (minEventLength < sampleInterval)
                sampleInterval = minEventLength;

            // 对每个重叠事件进行分割，分割精细度为采样大小
            var allSplitEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            foreach (var formOverlapEvent in overlapFormEvents)
            {
                var splitStartBeat = formOverlapEvent.StartBeat;
                while (splitStartBeat < formOverlapEvent.EndBeat)
                {
                    var splitEndBeat = splitStartBeat + sampleInterval;
                    if (splitEndBeat > formOverlapEvent.EndBeat)
                        splitEndBeat = formOverlapEvent.EndBeat;

                    var splitEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = splitStartBeat,
                        EndBeat = splitEndBeat,
                        StartValue = formOverlapEvent.GetValueAtBeat(splitStartBeat),
                        EndValue = formOverlapEvent.GetValueAtBeat(splitEndBeat)
                    };
                    allSplitEvents.Add(splitEvent);

                    splitStartBeat = splitEndBeat;
                }
            }

            overlapFormEvents = allSplitEvents;

            // 对toEvents中的重叠事件也进行分割
            allSplitEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            foreach (var toOverlapEvent in overlapToEvents)
            {
                var splitStartBeat = toOverlapEvent.StartBeat;
                while (splitStartBeat < toOverlapEvent.EndBeat)
                {
                    var splitEndBeat = splitStartBeat + sampleInterval;
                    if (splitEndBeat > toOverlapEvent.EndBeat)
                        splitEndBeat = toOverlapEvent.EndBeat;
                    var splitEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = splitStartBeat,
                        EndBeat = splitEndBeat,
                        StartValue = toOverlapEvent.GetValueAtBeat(splitStartBeat),
                        EndValue = toOverlapEvent.GetValueAtBeat(splitEndBeat)
                    };
                    allSplitEvents.Add(splitEvent);
                    splitStartBeat = splitEndBeat;
                }
            }

            overlapToEvents = allSplitEvents;

            // 由于当前层级的值为所有层级同类型事件值的总和，所以如果有重合事件，则需要在采样点上将两个事件的值相加，生成新的事件列表、且会影响到采样区间以外的所有事件
            // 将overlapToEvents和toEvents合并
            toEvents.AddRange(overlapToEvents);
            // 将overlapFormEvents和formEvents合并
            formEvents = overlapFormEvents;
            // 将formEvents的开始采样区间到结束采样区间没有事件的地方填充事件，事件数值头尾相等且为上一个事件的结束值（没有上一个事件则为0），每个事件的长度为sampleInterval
            // 将formEvents的开始采样区间到结束采样区间没有事件的地方填充事件，事件数值头尾相等且为上一个事件的结束值（没有上一个事件则为0），每个事件的长度为sampleInterval
            var filledFormEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();
            var fillBeat = sampleStartBeat;
            T lastValue = default;

            while (fillBeat < sampleEndBeat)
            {
                var fillEndBeat = fillBeat + sampleInterval;
                if (fillEndBeat > sampleEndBeat)
                    fillEndBeat = sampleEndBeat;

                // 检查当前区间是否已有事件
                var hasEvent = formEvents.Any(e => fillBeat >= e.StartBeat && fillBeat < e.EndBeat);

                if (hasEvent)
                {
                    // 如果有事件，获取该事件并更新lastValue
                    var existingEvent = formEvents.First(e => fillBeat >= e.StartBeat && fillBeat < e.EndBeat);
                    filledFormEvents.Add(existingEvent);
                    lastValue = existingEvent.EndValue;
                    fillBeat = existingEvent.EndBeat;
                }
                else
                {
                    // 如果没有事件，创建填充事件
                    var fillEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = fillBeat,
                        EndBeat = fillEndBeat,
                        StartValue = lastValue,
                        EndValue = lastValue
                    };
                    filledFormEvents.Add(fillEvent);
                    fillBeat = fillEndBeat;
                }
            }

            formEvents = filledFormEvents;
            // 将toEvents和formEvents的同开始结束拍的事件值相加，得到新的事件并替换toEvents中对应的事件
            var newToEvents = new List<Core.RePhiEdit.RePhiEdit.Event<T>>();

            // 遍历采样区间内的每个拍点
            var currentSampleBeat = sampleStartBeat;
            while (currentSampleBeat < sampleEndBeat)
            {
                var nextSampleBeat = currentSampleBeat + sampleInterval;
                if (nextSampleBeat > sampleEndBeat)
                    nextSampleBeat = sampleEndBeat;

                // 在toEvents中查找当前采样点的事件
                var toEvent = toEvents.FirstOrDefault(e =>
                    currentSampleBeat >= e.StartBeat && currentSampleBeat < e.EndBeat);

                // 在formEvents中查找当前采样点的事件
                var formEvent = formEvents.FirstOrDefault(e =>
                    currentSampleBeat >= e.StartBeat && currentSampleBeat < e.EndBeat);

                if (toEvent != null && formEvent != null)
                {
                    // 获取两个事件在采样点的值
                    var toStartValue = toEvent.GetValueAtBeat(currentSampleBeat);
                    var toEndValue = toEvent.GetValueAtBeat(nextSampleBeat);
                    var formStartValue = formEvent.GetValueAtBeat(currentSampleBeat);
                    var formEndValue = formEvent.GetValueAtBeat(nextSampleBeat);

                    // 使用dynamic进行值相加
                    dynamic newStartValue = toStartValue;
                    dynamic newEndValue = toEndValue;
                    newStartValue += (dynamic)formStartValue;
                    newEndValue += (dynamic)formEndValue;

                    // 创建新事件
                    var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = currentSampleBeat,
                        EndBeat = nextSampleBeat,
                        StartValue = (T)newStartValue,
                        EndValue = (T)newEndValue
                    };
                    newToEvents.Add(newEvent);
                }
                else if (toEvent != null)
                {
                    // 只有toEvent存在，需要叠加currentSampleBeat前最后一个formEvent的EndValue
                    var lastFormEvent = formEvents.LastOrDefault(e => e.EndBeat <= currentSampleBeat);
                    dynamic formCarryValue = lastFormEvent != null ? (dynamic)lastFormEvent.EndValue : default(T);
                    
                    var toStartValue = toEvent.GetValueAtBeat(currentSampleBeat);
                    var toEndValue = toEvent.GetValueAtBeat(nextSampleBeat);
                    
                    dynamic newStartValue = toStartValue;
                    dynamic newEndValue = toEndValue;
                    newStartValue += formCarryValue;
                    newEndValue += formCarryValue;
                    
                    var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = currentSampleBeat,
                        EndBeat = nextSampleBeat,
                        StartValue = (T)newStartValue,
                        EndValue = (T)newEndValue
                    };
                    newToEvents.Add(newEvent);
                }
                else if (formEvent != null)
                {
                    // 只有formEvent存在，需要叠加currentSampleBeat前最后一个toEvent的EndValue
                    var lastToEvent = toEvents.LastOrDefault(e => e.EndBeat <= currentSampleBeat);
                    dynamic toCarryValue = lastToEvent != null ? (dynamic)lastToEvent.EndValue : default(T);
                    
                    var formStartValue = formEvent.GetValueAtBeat(currentSampleBeat);
                    var formEndValue = formEvent.GetValueAtBeat(nextSampleBeat);
                    
                    dynamic newStartValue = formStartValue;
                    dynamic newEndValue = formEndValue;
                    newStartValue += toCarryValue;
                    newEndValue += toCarryValue;
                    
                    var newEvent = new Core.RePhiEdit.RePhiEdit.Event<T>
                    {
                        StartBeat = currentSampleBeat,
                        EndBeat = nextSampleBeat,
                        StartValue = (T)newStartValue,
                        EndValue = (T)newEndValue
                    };
                    newToEvents.Add(newEvent);
                }

                currentSampleBeat = nextSampleBeat;
            }

            // 计算formEvents在采样区间内的累积值变化
            dynamic carryValue = default(T);
            if (formEvents.Any())
            {
                var lastFormInSample = formEvents.LastOrDefault(e => e.EndBeat <= sampleEndBeat);
                if (lastFormInSample != null)
                    carryValue = lastFormInSample.EndValue;
            }

            // 替换第311-326行的逻辑
            // 对采样区间后的所有toEvents累加formCarryValue
            foreach (var e in toEvents)
            {
                if (e.StartBeat >= sampleEndBeat)
                {
                    dynamic newStartValue = (dynamic)e.StartValue + carryValue;
                    dynamic newEndValue = (dynamic)e.EndValue + carryValue;
                    e.StartValue = (T)newStartValue;
                    e.EndValue = (T)newEndValue;
                }
            }

            // 删除toEvents中采样区间的所有事件，防止重叠
            toEvents.RemoveAll(e => e.StartBeat < sampleEndBeat && e.EndBeat > sampleStartBeat);
            // 将newToEvents中的事件合并到toEvents中
            toEvents.AddRange(newToEvents);

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