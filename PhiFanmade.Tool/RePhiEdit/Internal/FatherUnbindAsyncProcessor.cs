using System.Collections.Concurrent;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

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
    /// <param name="precision">切割精度，默认64分之一拍</param>
    /// <param name="tolerance">拟合容差，越大拟合精细度越低</param>
    /// <returns></returns>
    public static async Task<Rpe.JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        return await Task.Run(() => FatherUnbindCore(targetJudgeLineIndex, allJudgeLines, precision, tolerance));
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。(异步多线程+自适应采样版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">切割精度，默认64分之一拍</param>
    /// <param name="tolerance">拟合容差，越大拟合精细度越低</param>
    /// <returns></returns>
    public static async Task<Rpe.JudgeLine> FatherUnbindAsyncPlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        return await Task.Run(() => FatherUnbindCorePlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance));
    }

    public static Rpe.JudgeLine FatherUnbindCore(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
                // 递归解绑父线自身（传入父线的索引，得到父线解绑后的绝对坐标），而非传入祖父线索引
                fatherLineCopy = FatherUnbindCore(judgeLineCopy.Father, allJudgeLinesCopy);

            // 并行合并事件层级前,先移除无用层级
            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;
            // 顺序合并事件层级（不可并行：EventMerge 不满足交换律，层级顺序影响最终数值）
            var targetLineNewXevents = new List<Rpe.Event<float>>();
            var targetLineNewYevents = new List<Rpe.Event<float>>();
            var fatherLineNewXevents = new List<Rpe.Event<float>>();
            var fatherLineNewYevents = new List<Rpe.Event<float>>();
            var fatherLineNewRotateEvents = new List<Rpe.Event<float>>();

            // 按层级顺序逐层合并（顺序不可颠倒：EventMerge 不满足交换律）
            // 若乱序合并（如 ConcurrentBag + Parallel.ForEach），当 Layer A 覆盖 1~10、Layer B 覆盖 10~20 时，
            // Layer B 的数值将缺失 Layer A 末值的偏移，导致绝对坐标计算错误。
            foreach (var layer in judgeLineCopy.EventLayers)
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    targetLineNewXevents = EventProcessor.EventMerge(targetLineNewXevents, layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    targetLineNewYevents = EventProcessor.EventMerge(targetLineNewYevents, layer.MoveYEvents);
            }

            // 按层级顺序逐层合并父线事件
            foreach (var layer in fatherLineCopy.EventLayers)
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    fatherLineNewXevents = EventProcessor.EventMerge(fatherLineNewXevents, layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    fatherLineNewYevents = EventProcessor.EventMerge(fatherLineNewYevents, layer.MoveYEvents);
                if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                    fatherLineNewRotateEvents = EventProcessor.EventMerge(fatherLineNewRotateEvents, layer.RotateEvents);
            }

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

            // 闭包捕获要求变量不可变，创建局部引用以防止外部重赋值影响 Task 内部
            var yevents = fatherLineNewYevents;
            var xevents = fatherLineNewXevents;
            var newYevents = targetLineNewYevents;
            var newXevents = targetLineNewXevents;
            var rotateEvents = fatherLineNewRotateEvents;
            // 并行切割事件
            var cutTasks = new[]
            {
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(newXevents, targetLineXEventsMinBeat!,
                        targetLineXEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(newYevents, targetLineYEventsMinBeat!,
                        targetLineYEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(xevents, fatherLineXEventsMinBeat!,
                        fatherLineXEventsMaxBeat!)),
                Task.Run(() =>
                    EventProcessor.CutEventsInRange(yevents, fatherLineYEventsMinBeat!,
                        fatherLineYEventsMaxBeat!)),
                Task.Run(() => EventProcessor.CutEventsInRange(rotateEvents,
                    fatherLineRotateEventsMinBeat!,
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
            var cutLength = new Beat(1d / precision);

            // 并行处理每个拍段
            var currentBeat = overallMinBeat;
            var beatSegments = new List<Beat>();
            while (currentBeat <= overallMaxBeat)
            {
                beatSegments.Add(currentBeat);
                currentBeat += cutLength;
            }

            var targetLineXEventResult = new ConcurrentBag<(int index, Rpe.Event<float> evt)>();
            var targetLineYEventResult = new ConcurrentBag<(int index, Rpe.Event<float> evt)>();

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

                var newXEvent = new Rpe.Event<float>
                {
                    StartBeat = beat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartX,
                    EndValue = (float)absEndX,
                };
                var newYEvent = new Rpe.Event<float>
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

            // 确保至少有一个层级存在
            if (judgeLineCopy.EventLayers.Count == 0)
            {
                judgeLineCopy.EventLayers.Add(new Rpe.EventLayer());
            }

            // 赋值压缩后的事件列表
            judgeLineCopy.EventLayers[0].MoveXEvents = EventProcessor.EventListCompress(sortedXEvents, tolerance);
            judgeLineCopy.EventLayers[0].MoveYEvents = EventProcessor.EventListCompress(sortedYEvents, tolerance);

            if (judgeLineCopy.RotateWithFather)
            {
                judgeLineCopy.EventLayers[0].RotateEvents =
                    EventProcessor.EventListCompress(
                        EventProcessor.EventMerge(judgeLineCopy.EventLayers[0].RotateEvents,
                            fatherLineNewRotateEvents), tolerance);
            }

            judgeLineCopy.Father = -1;
            return judgeLineCopy;
        }
        catch (NullReferenceException nullReferenceException)
        {
            RePhiEditHelper.OnError.Invoke("FatherUnbind: It seems that something is null:" + nullReferenceException);
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            RePhiEditHelper.OnError.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。(自适应采样节省性能版本)
    /// </summary>
    public static Rpe.JudgeLine FatherUnbindCorePlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
                // 递归解绑父线自身（传入父线的索引，得到父线解绑后的绝对坐标），而非传入祖父线索引
                fatherLineCopy = FatherUnbindCorePlus(judgeLineCopy.Father, allJudgeLinesCopy);

            // 合并事件层级前，先移除无用层级
            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;

            // 顺序合并事件层级（不可并行：EventMergePlus 不满足交换律，层级顺序影响最终数值）
            var targetLineNewXevents = new List<Rpe.Event<float>>();
            var targetLineNewYevents = new List<Rpe.Event<float>>();
            var fatherLineNewXevents = new List<Rpe.Event<float>>();
            var fatherLineNewYevents = new List<Rpe.Event<float>>();
            var fatherLineNewRotateEvents = new List<Rpe.Event<float>>();

            foreach (var layer in judgeLineCopy.EventLayers)
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    targetLineNewXevents = EventProcessor.EventMergePlus(targetLineNewXevents, layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    targetLineNewYevents = EventProcessor.EventMergePlus(targetLineNewYevents, layer.MoveYEvents);
            }

            foreach (var layer in fatherLineCopy.EventLayers)
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    fatherLineNewXevents = EventProcessor.EventMergePlus(fatherLineNewXevents, layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    fatherLineNewYevents = EventProcessor.EventMergePlus(fatherLineNewYevents, layer.MoveYEvents);
                if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                    fatherLineNewRotateEvents =
                        EventProcessor.EventMergePlus(fatherLineNewRotateEvents, layer.RotateEvents);
            }

            // 计算 X/Y 移动事件的总体拍范围（与原始逻辑一致，旋转事件不决定范围）
            var allXYLists = new[]
                { targetLineNewXevents, targetLineNewYevents, fatherLineNewXevents, fatherLineNewYevents };
            Beat overallMinBeat = new Beat(0), overallMaxBeat = new Beat(0);
            var hasEvents = false;
            foreach (var list in allXYLists)
            {
                if (list.Count == 0) continue;
                var mn = list.Min(e => e.StartBeat);
                var mx = list.Max(e => e.EndBeat);
                if (!hasEvents)
                {
                    overallMinBeat = mn;
                    overallMaxBeat = mx;
                    hasEvents = true;
                }
                else
                {
                    if (mn < overallMinBeat) overallMinBeat = mn;
                    if (mx > overallMaxBeat) overallMaxBeat = mx;
                }
            }

            if (!hasEvents)
            {
                judgeLineCopy.Father = -1;
                return judgeLineCopy;
            }

            var cutLength = new Beat(1d / precision);

            // 收集所有事件边界拍（含旋转）作为强制切割点，保证跨事件时不出现虚假线性
            var allEventLists = new[]
            {
                targetLineNewXevents, targetLineNewYevents,
                fatherLineNewXevents, fatherLineNewYevents, fatherLineNewRotateEvents
            };
            var keyBeatsList = new List<Beat> { overallMinBeat, overallMaxBeat };
            foreach (var list in allEventLists)
                foreach (var e in list)
                {
                    if (e.StartBeat >= overallMinBeat && e.StartBeat <= overallMaxBeat)
                        keyBeatsList.Add(e.StartBeat);
                    if (e.EndBeat >= overallMinBeat && e.EndBeat <= overallMaxBeat)
                        keyBeatsList.Add(e.EndBeat);
                }

            var keyBeats = keyBeatsList.Distinct().OrderBy(b => b).ToList();

            // 辅助：从事件列表查询指定拍的插值数值（传入语义）
            // 在 beat 处优先取"从 beat 开始或跨越 beat 的活跃事件"（StartBeat <= beat < EndBeat），
            // 即新区间起点时取新事件的 StartValue。
            float GetValIn(List<Rpe.Event<float>> events, Beat beat)
            {
                if (events.Count == 0) return 0f;
                var active = events.Where(e => e.StartBeat <= beat && e.EndBeat > beat)
                    .MaxBy(e => e.StartBeat);
                if (active != null) return active.GetValueAtBeat(beat);
                var prev = events.FindLast(e => e.EndBeat <= beat);
                return prev?.EndValue ?? 0f;
            }

            // 辅助：从事件列表查询指定拍的插值数值（传出语义）
            // 在 beat 处优先取"在 beat 之前就已开始、在 beat 处结束或越过 beat 的事件"
            // （StartBeat < beat && EndBeat >= beat），即区间终点时取旧事件的 EndValue，
            // 而非下一个新事件的 StartValue，避免在事件跳变边界产生虚假连贯。
            float GetValOut(List<Rpe.Event<float>> events, Beat beat)
            {
                if (events.Count == 0) return 0f;
                var active = events.Where(e => e.StartBeat < beat && e.EndBeat >= beat)
                    .MaxBy(e => e.StartBeat);
                if (active != null) return active.GetValueAtBeat(beat);
                // 回退：取最近已结束的事件的 EndValue
                var prev = events.FindLast(e => e.EndBeat <= beat);
                return prev?.EndValue ?? 0f;
            }

            // 计算某拍处的绝对坐标（传入语义：用于区间起点 / 段起点）
            (double absX, double absY) GetAbsPos(Beat beat)
            {
                var tx = GetValIn(targetLineNewXevents, beat);
                var ty = GetValIn(targetLineNewYevents, beat);
                var fx = GetValIn(fatherLineNewXevents, beat);
                var fy = GetValIn(fatherLineNewYevents, beat);
                var fr = GetValIn(fatherLineNewRotateEvents, beat);
                return FatherUnbindProcessor.GetLinePos(fx, fy, fr, tx, ty);
            }

            // 计算某拍处的绝对坐标（传出语义：用于区间终点 / 段终点）
            (double absX, double absY) GetAbsPosOut(Beat beat)
            {
                var tx = GetValOut(targetLineNewXevents, beat);
                var ty = GetValOut(targetLineNewYevents, beat);
                var fx = GetValOut(fatherLineNewXevents, beat);
                var fy = GetValOut(fatherLineNewYevents, beat);
                var fr = GetValOut(fatherLineNewRotateEvents, beat);
                return FatherUnbindProcessor.GetLinePos(fx, fy, fr, tx, ty);
            }

            var resultXEvents = new List<Rpe.Event<float>>();
            var resultYEvents = new List<Rpe.Event<float>>();

            // 对每个关键拍区间进行自适应采样（参照 EventMergePlus 的误差驱动策略）
            // 区间内所有输入事件均为线性，但 GetLinePos 引入旋转非线性，故仍需误差检测
            for (var ki = 0; ki < keyBeats.Count - 1; ki++)
            {
                var intervalStart = keyBeats[ki];
                var intervalEnd = keyBeats[ki + 1];
                if (intervalStart >= intervalEnd) continue;

                // 预计算区间终点的传出绝对坐标，用于线性预测
                // 使用传出语义：取旧事件在 intervalEnd 处的结束值，而非新事件的起始值
                var (endAbsX, endAbsY) = GetAbsPosOut(intervalEnd);

                var segStart = intervalStart;
                var (segStartAbsX, segStartAbsY) = GetAbsPos(intervalStart);

                var currentBeat = intervalStart;
                while (currentBeat < intervalEnd)
                {
                    var nextBeat = currentBeat + cutLength;
                    if (nextBeat > intervalEnd) nextBeat = intervalEnd;
                    var isLast = nextBeat >= intervalEnd;

                    // 最后一步直接复用已以传出语义计算好的 endAbsX/Y，
                    // 避免再次以传入语义 GetAbsPos(intervalEnd) 取到新事件的 StartValue
                    var (absXAtNext, absYAtNext) = isLast ? (endAbsX, endAbsY) : GetAbsPos(nextBeat);

                    var shouldCut = isLast;
                    if (!isLast)
                    {
                        // 线性预测：从 segStart 到 intervalEnd 线性内插，估算 nextBeat 处的预期值
                        var segLen = (double)(intervalEnd - segStart);
                        var progress = segLen > 1e-12
                            ? (double)(nextBeat - segStart) / segLen
                            : 1.0;
                        var predictedX = segStartAbsX + (endAbsX - segStartAbsX) * progress;
                        var predictedY = segStartAbsY + (endAbsY - segStartAbsY) * progress;

                        var errorX = Math.Abs(absXAtNext - predictedX);
                        var errorY = Math.Abs(absYAtNext - predictedY);

                        // 以当前段首尾均值为基准，按 tolerance% 计算容差
                        var thresholdX =
                            tolerance / 100.0 * ((Math.Abs(segStartAbsX) + Math.Abs(absXAtNext)) / 2.0 + 1e-9);
                        var thresholdY =
                            tolerance / 100.0 * ((Math.Abs(segStartAbsY) + Math.Abs(absYAtNext)) / 2.0 + 1e-9);
                        shouldCut = errorX > thresholdX || errorY > thresholdY;
                    }

                    if (shouldCut)
                    {
                        resultXEvents.Add(new Rpe.Event<float>
                        {
                            StartBeat = segStart,
                            EndBeat = nextBeat,
                            StartValue = (float)segStartAbsX,
                            EndValue = (float)absXAtNext,
                        });
                        resultYEvents.Add(new Rpe.Event<float>
                        {
                            StartBeat = segStart,
                            EndBeat = nextBeat,
                            StartValue = (float)segStartAbsY,
                            EndValue = (float)absYAtNext,
                        });
                        segStart = nextBeat;
                        segStartAbsX = absXAtNext;
                        segStartAbsY = absYAtNext;
                        // endAbsX/Y 固定为区间终点，不随 segStart 变化
                    }

                    currentBeat = nextBeat;
                }
            }

            // 清除其它层级的 MoveX/MoveY
            for (var i = 1; i < judgeLineCopy.EventLayers.Count; i++)
            {
                judgeLineCopy.EventLayers[i].MoveXEvents.Clear();
                judgeLineCopy.EventLayers[i].MoveYEvents.Clear();
            }

            // 确保至少有一个层级存在
            if (judgeLineCopy.EventLayers.Count == 0)
                judgeLineCopy.EventLayers.Add(new Rpe.EventLayer());

            // 赋值压缩后的事件列表
            judgeLineCopy.EventLayers[0].MoveXEvents = EventProcessor.EventListCompress(resultXEvents, tolerance);
            judgeLineCopy.EventLayers[0].MoveYEvents = EventProcessor.EventListCompress(resultYEvents, tolerance);

            if (judgeLineCopy.RotateWithFather)
            {
                judgeLineCopy.EventLayers[0].RotateEvents =
                    EventProcessor.EventListCompress(
                        EventProcessor.EventMergePlus(judgeLineCopy.EventLayers[0].RotateEvents,
                            fatherLineNewRotateEvents), tolerance);
            }

            judgeLineCopy.Father = -1;
            return judgeLineCopy;
        }
        catch (NullReferenceException nullReferenceException)
        {
            RePhiEditHelper.OnError.Invoke("FatherUnbind: It seems that something is null:" + nullReferenceException);
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            RePhiEditHelper.OnError.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }
}

