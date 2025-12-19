using System.Collections.Concurrent;
using static PhiFanmade.Core.RePhiEdit.RePhiEdit;
namespace PhiFanmade.OpenTool.Utils.RePhiEditUtility;

/// <summary>
/// 判定线父子关系异步处理器（多线程版本）
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static async Task<JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines)
    {
        return await Task.Run(() => FatherUnbindCore(targetJudgeLineIndex, allJudgeLines));
    }

    public static JudgeLine FatherUnbindCore(int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                //RePhiEditUtility.OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
                fatherLineCopy = FatherUnbindCore(fatherLineCopy.Father, allJudgeLinesCopy);

            // 并行合并事件层级前,先移除无用层级
            judgeLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;
            // 并行合并事件层级
            var targetLineNewXevents = new List<Event<float>>();
            var targetLineNewYevents = new List<Event<float>>();
            var fatherLineNewXevents = new List<Event<float>>();
            var fatherLineNewYevents = new List<Event<float>>();
            var fatherLineNewRotateEvents = new List<Event<float>>();

            // 使用 ConcurrentBag 来收集并行处理的结果
            var targetXBag = new ConcurrentBag<List<Event<float>>>();
            var targetYBag = new ConcurrentBag<List<Event<float>>>();

            Parallel.ForEach(judgeLineCopy.EventLayers, layer =>
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    targetXBag.Add(layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    targetYBag.Add(layer.MoveYEvents);
            });

            foreach (var events in targetXBag)
                targetLineNewXevents = EventProcessor.EventMerge(targetLineNewXevents, events);
            foreach (var events in targetYBag)
                targetLineNewYevents = EventProcessor.EventMerge(targetLineNewYevents, events);

            // 并行处理父线事件
            var fatherXBag = new ConcurrentBag<List<Event<float>>>();
            var fatherYBag = new ConcurrentBag<List<Event<float>>>();
            var fatherRotateBag = new ConcurrentBag<List<Event<float>>>();

            Parallel.ForEach(fatherLineCopy.EventLayers, layer =>
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    fatherXBag.Add(layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    fatherYBag.Add(layer.MoveYEvents);
                if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                    fatherRotateBag.Add(layer.RotateEvents);
            });

            foreach (var events in fatherXBag)
                fatherLineNewXevents = EventProcessor.EventMerge(fatherLineNewXevents, events);
            foreach (var events in fatherYBag)
                fatherLineNewYevents = EventProcessor.EventMerge(fatherLineNewYevents, events);
            foreach (var events in fatherRotateBag)
                fatherLineNewRotateEvents = EventProcessor.EventMerge(fatherLineNewRotateEvents, events);

            // 计算拍范围
            var targetLineXEventsMinBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Min(e => e.StartBeat)
                : new Beat(0);
            var targetLineXEventsMaxBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Max(e => e.EndBeat)
                : new Beat(0);
            var targetLineYEventsMinBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Min(e => e.StartBeat)
                : new Beat(0);
            var targetLineYEventsMaxBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Max(e => e.EndBeat)
                : new Beat(0);

            var fatherLineXEventsMinBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Min(e => e.StartBeat)
                : new Beat(0);
            var fatherLineXEventsMaxBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Max(e => e.EndBeat)
                : new Beat(0);
            var fatherLineYEventsMinBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Min(e => e.StartBeat)
                : new Beat(0);
            var fatherLineYEventsMaxBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Max(e => e.EndBeat)
                : new Beat(0);
            var fatherLineRotateEventsMinBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Min(e => e.StartBeat)
                : new Beat(0);
            var fatherLineRotateEventsMaxBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Max(e => e.EndBeat)
                : new Beat(0);

            // 并行切割事件
            var cutTasks = new[]
            {
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(targetLineNewXevents, targetLineXEventsMinBeat!, targetLineXEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(targetLineNewYevents, targetLineYEventsMinBeat!, targetLineYEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(fatherLineNewXevents, fatherLineXEventsMinBeat!, fatherLineXEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(fatherLineNewYevents, fatherLineYEventsMinBeat!, fatherLineYEventsMaxBeat!)),
                Task.Run(() => EventProcessor.CutEventsInRange(fatherLineNewRotateEvents, fatherLineRotateEventsMinBeat!,
                    fatherLineRotateEventsMaxBeat!))
            };

            Task.WaitAll(cutTasks);

            targetLineNewXevents = cutTasks[0].Result;
            targetLineNewYevents = cutTasks[1].Result;
            fatherLineNewXevents = cutTasks[2].Result;
            fatherLineNewYevents = cutTasks[3].Result;
            fatherLineNewRotateEvents = cutTasks[4].Result;

            var overallMinBeat = new Beat(Math.Min(
                Math.Min(targetLineXEventsMinBeat!, targetLineYEventsMinBeat!),
                Math.Min(fatherLineXEventsMinBeat!, fatherLineYEventsMinBeat!)));
            var overallMaxBeat = new Beat(Math.Max(
                Math.Max(targetLineXEventsMaxBeat!, targetLineYEventsMaxBeat!),
                Math.Max(fatherLineXEventsMaxBeat!, fatherLineYEventsMaxBeat!)));
            var cutLength = new Beat(1d / 64d);

            // 并行处理每个拍段
            var currentBeat = overallMinBeat;
            var beatSegments = new List<Beat>();
            while (currentBeat <= overallMaxBeat)
            {
                beatSegments.Add(currentBeat);
                currentBeat += cutLength;
            }

            var targetLineXEventResult = new ConcurrentBag<(int index, Event<float> evt)>();
            var targetLineYEventResult = new ConcurrentBag<(int index, Event<float> evt)>();

            Parallel.For(0, beatSegments.Count, i =>
            {
                var beat = beatSegments[i];
                var nextBeat = beat + cutLength;

                var previousFatherXValue = 0.0f;
                var previousFatherYValue = 0.0f;
                var previousFatherRotateValue = 0.0f;
                var previousTargetXValue = 0.0f;
                var previousTargetYValue = 0.0f;

                // 获取之前的值
                if (i > 0)
                {
                    var prevFatherX = fatherLineNewXevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevFatherY = fatherLineNewYevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevFatherRotate = fatherLineNewRotateEvents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevTargetX = targetLineNewXevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevTargetY = targetLineNewYevents.LastOrDefault(e => e.EndBeat <= beat);

                    previousFatherXValue = prevFatherX?.EndValue ?? 0.0f;
                    previousFatherYValue = prevFatherY?.EndValue ?? 0.0f;
                    previousFatherRotateValue = prevFatherRotate?.EndValue ?? 0.0f;
                    previousTargetXValue = prevTargetX?.EndValue ?? 0.0f;
                    previousTargetYValue = prevTargetY?.EndValue ?? 0.0f;
                }

                var fatherXevent =
                    fatherLineNewXevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherYevent =
                    fatherLineNewYevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherXStartValue = fatherXevent != null ? fatherXevent.StartValue : previousFatherXValue;
                var fatherYStartValue = fatherYevent != null ? fatherYevent.StartValue : previousFatherYValue;
                var fatherXEndValue = fatherXevent != null ? fatherXevent.EndValue : previousFatherXValue;
                var fatherYEndValue = fatherYevent != null ? fatherYevent.EndValue : previousFatherYValue;

                var fatherRotateEvent =
                    fatherLineNewRotateEvents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherRotateStartValue =
                    fatherRotateEvent != null ? fatherRotateEvent.StartValue : previousFatherRotateValue;
                var fatherRotateEndValue =
                    fatherRotateEvent != null ? fatherRotateEvent.EndValue : previousFatherRotateValue;

                var targetXevent =
                    targetLineNewXevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var targetYevent =
                    targetLineNewYevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var targetXStartValue = targetXevent != null ? targetXevent.StartValue : previousTargetXValue;
                var targetYStartValue = targetYevent != null ? targetYevent.StartValue : previousTargetYValue;
                var targetXEndValue = targetXevent != null ? targetXevent.EndValue : previousTargetXValue;
                var targetYEndValue = targetYevent != null ? targetYevent.EndValue : previousTargetYValue;

                var (absStartX, absStartY) = FatherUnbindProcessor.GetLinePos(fatherXStartValue, fatherYStartValue,
                    fatherRotateStartValue, targetXStartValue, targetYStartValue);
                var (absEndX, absEndY) = FatherUnbindProcessor.GetLinePos(fatherXEndValue, fatherYEndValue,
                    fatherRotateEndValue, targetXEndValue, targetYEndValue);

                var newXEvent = new Event<float>
                {
                    StartBeat = beat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartX,
                    EndValue = (float)absEndX,
                };
                var newYEvent = new Event<float>
                {
                    StartBeat = beat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartY,
                    EndValue = (float)absEndY,
                };

                targetLineXEventResult.Add((i, newXEvent));
                targetLineYEventResult.Add((i, newYEvent));
            });

            // 排序并转换为列表
            var sortedXEvents = targetLineXEventResult.OrderBy(x => x.index).Select(x => x.evt).ToList();
            var sortedYEvents = targetLineYEventResult.OrderBy(x => x.index).Select(x => x.evt).ToList();

            // 清除其它层级
            for (var i = 1; i < judgeLineCopy.EventLayers.Count; i++)
            {
                judgeLineCopy.EventLayers[i].MoveXEvents.Clear();
                judgeLineCopy.EventLayers[i].MoveYEvents.Clear();
            }

            if (judgeLineCopy.EventLayers.Count == 0)
            {
                judgeLineCopy.EventLayers.Add(new EventLayer());
            }

            judgeLineCopy.EventLayers[0].MoveXEvents = EventProcessor.EventListCompress(sortedXEvents);
            judgeLineCopy.EventLayers[0].MoveYEvents = EventProcessor.EventListCompress(sortedYEvents);

            if (judgeLineCopy.RotateWithFather)
            {
                judgeLineCopy.EventLayers[0].RotateEvents =
                    EventProcessor.EventListCompress(EventProcessor.EventMerge(judgeLineCopy.EventLayers[0].RotateEvents, fatherLineNewRotateEvents));
            }

            judgeLineCopy.Father = -1;
            return judgeLineCopy;
        }
        catch (NullReferenceException)
        {
            //RePhiEditUtility.OnWarning.Invoke("FatherUnbind: It seems that something is null.");
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            //RePhiEditUtility.OnWarning.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }
}

