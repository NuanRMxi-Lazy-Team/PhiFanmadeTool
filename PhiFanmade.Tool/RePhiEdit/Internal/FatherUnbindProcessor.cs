using System.Collections.Concurrent;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

/// <summary>
/// 判定线父子关系处理器
/// </summary>
internal static class FatherUnbindProcessor
{
    /// <summary>
    /// 在有父线的情况下，根据父线的绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    /// <param name="fatherLineX">父线绝对 X 坐标</param>
    /// <param name="fatherLineY">父线绝对 Y 坐标</param>
    /// <param name="angleDegrees">父线旋转角度（度）</param>
    /// <param name="lineX">子线相对于父线的 X 偏移</param>
    /// <param name="lineY">子线相对于父线的 Y 偏移</param>
    /// <returns>子线绝对坐标 (X, Y)</returns>
    internal static (double, double) GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        double rad = (angleDegrees % 360) * Math.PI / 180d;
        double rotX = lineX * Math.Cos(rad) + lineY * Math.Sin(rad);
        double rotY = -lineX * Math.Sin(rad) + lineY * Math.Cos(rad);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// </summary>
    public static Rpe.JudgeLine FatherUnbind(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d) =>
        FatherUnbindCore(targetJudgeLineIndex, allJudgeLines, precision, tolerance);

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。（自适应采样节省性能版本）
    /// </summary>
    public static Rpe.JudgeLine FatherUnbindPlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d) =>
        FatherUnbindCorePlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance);

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// 策略：等间隔采样（精度由 precision 决定），多线程并行计算各采样段的绝对坐标。
    /// </summary>
    internal static Rpe.JudgeLine FatherUnbindCore(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbind[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = FatherUnbindCore(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，可并行）
            var tX = MergeLayerChannel(tLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMerge(a, b));
            var tY = MergeLayerChannel(tLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMerge(a, b));
            var fX = MergeLayerChannel(fLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMerge(a, b));
            var fY = MergeLayerChannel(fLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMerge(a, b));
            var fR = MergeLayerChannel(fLayers, l => l.RotateEvents, (a, b) => EventProcessor.EventMerge(a, b));

            var (txMin, txMax) = GetEventRange(tX);
            var (tyMin, tyMax) = GetEventRange(tY);
            var (fxMin, fxMax) = GetEventRange(fX);
            var (fyMin, fyMax) = GetEventRange(fY);
            var (frMin, frMax) = GetEventRange(fR);

            // 5 个通道互不依赖，并行切割为等长小段
            var cutTasks = new[]
            {
                Task.Run(() => EventProcessor.CutEventsInRange(tX, txMin, txMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(tY, tyMin, tyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fX, fxMin, fxMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fY, fyMin, fyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fR, frMin, frMax))
            };
            Task.WaitAll(cutTasks);

            tX = cutTasks[0].Result;
            tY = cutTasks[1].Result;
            fX = cutTasks[2].Result;
            fY = cutTasks[3].Result;
            fR = cutTasks[4].Result;

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            var overallMin = new Beat(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)));
            var overallMax = new Beat(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)));
            var step = new Beat(1d / precision);

            var beats = new List<Beat>();
            for (var b = overallMin; b <= overallMax; b += step)
                beats.Add(b);

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            var xBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();
            var yBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();

            Parallel.For(0, beats.Count, i =>
            {
                var beat = beats[i];
                var next = beat + step;

                // 无事件覆盖时，取当前拍之前最近一个结束事件的末尾值作为默认值
                var prevFX = i > 0 ? fX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevFY = i > 0 ? fY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevFR = i > 0 ? fR.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevTX = i > 0 ? tX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevTY = i > 0 ? tY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;

                var fxEvt = fX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var fyEvt = fY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var frEvt = fR.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var txEvt = tX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var tyEvt = tY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);

                var (startAbsX, startAbsY) = GetLinePos(
                    fxEvt?.StartValue ?? prevFX, fyEvt?.StartValue ?? prevFY, frEvt?.StartValue ?? prevFR,
                    txEvt?.StartValue ?? prevTX, tyEvt?.StartValue ?? prevTY);
                var (endAbsX, endAbsY) = GetLinePos(
                    fxEvt?.EndValue ?? prevFX, fyEvt?.EndValue ?? prevFY, frEvt?.EndValue ?? prevFR,
                    txEvt?.EndValue ?? prevTX, tyEvt?.EndValue ?? prevTY);

                xBag.Add((i, new Rpe.Event<float>
                    { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsX, EndValue = (float)endAbsX }));
                yBag.Add((i, new Rpe.Event<float>
                    { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsY, EndValue = (float)endAbsY }));
            });

            var sortedX = xBag.OrderBy(x => x.i).Select(x => x.evt).ToList();
            var sortedY = yBag.OrderBy(x => x.i).Select(x => x.evt).ToList();

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 采样完成，压缩并写回");
            WriteResultToLine(judgeLineCopy, sortedX, sortedY, fR, tolerance,
                (a, b) => EventProcessor.EventMerge(a, b));

            RePhiEditHelper.OnInfo.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// 策略：自适应采样——以事件边界为强制切割点，仅在误差超过容差时才插入新采样段。
    /// </summary>
    internal static Rpe.JudgeLine FatherUnbindCorePlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy =
                    FatherUnbindCorePlus(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，可并行）
            var tX = MergeLayerChannel(tLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b));
            var tY = MergeLayerChannel(tLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b));
            var fX = MergeLayerChannel(fLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b));
            var fY = MergeLayerChannel(fLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b));
            var fR = MergeLayerChannel(fLayers, l => l.RotateEvents, (a, b) => EventProcessor.EventMergePlus(a, b));

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            Beat overallMin = new Beat(0), overallMax = new Beat(0);
            var hasEvents = false;
            foreach (var list in new[] { tX, tY, fX, fY })
            {
                if (list.Count == 0) continue;
                var (mn, mx) = GetEventRange(list);
                if (!hasEvents) { overallMin = mn; overallMax = mx; hasEvents = true; }
                else { if (mn < overallMin) overallMin = mn; if (mx > overallMax) overallMax = mx; }
            }

            if (!hasEvents)
            {
                judgeLineCopy.Father = -1;
                return judgeLineCopy;
            }

            var step = new Beat(1d / precision);

            // 以所有通道的事件边界拍作为强制切割点，防止跳变被忽略
            var keyBeatsList = new List<Beat> { overallMin, overallMax };
            foreach (var list in new[] { tX, tY, fX, fY, fR })
                foreach (var e in list)
                {
                    if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                    if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
                }

            var keyBeats = keyBeatsList.Distinct().OrderBy(b => b).ToList();

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            // 传入语义：取在 beat 时刻正在生效的事件的插值（用于区间/段起点）
            float GetValIn(List<Rpe.Event<float>> events, Beat beat)
            {
                if (events.Count == 0) return 0f;
                var active = events.Where(e => e.StartBeat <= beat && e.EndBeat > beat).MaxBy(e => e.StartBeat);
                if (active != null) return active.GetValueAtBeat(beat);
                return events.FindLast(e => e.EndBeat <= beat)?.EndValue ?? 0f;
            }

            // 传出语义：取在 beat 时刻即将结束的事件的插值（用于区间/段终点，避免取到新事件 StartValue 造成虚假连贯）
            float GetValOut(List<Rpe.Event<float>> events, Beat beat)
            {
                if (events.Count == 0) return 0f;
                var active = events.Where(e => e.StartBeat < beat && e.EndBeat >= beat).MaxBy(e => e.StartBeat);
                if (active != null) return active.GetValueAtBeat(beat);
                return events.FindLast(e => e.EndBeat <= beat)?.EndValue ?? 0f;
            }

            (double X, double Y) AbsPosIn(Beat beat) => GetLinePos(
                GetValIn(fX, beat), GetValIn(fY, beat), GetValIn(fR, beat),
                GetValIn(tX, beat), GetValIn(tY, beat));

            (double X, double Y) AbsPosOut(Beat beat) => GetLinePos(
                GetValOut(fX, beat), GetValOut(fY, beat), GetValOut(fR, beat),
                GetValOut(tX, beat), GetValOut(tY, beat));

            var resultX = new List<Rpe.Event<float>>();
            var resultY = new List<Rpe.Event<float>>();

            for (var ki = 0; ki < keyBeats.Count - 1; ki++)
            {
                var iStart = keyBeats[ki];
                var iEnd = keyBeats[ki + 1];
                if (iStart >= iEnd) continue;

                // 区间终点使用传出语义，取旧事件末尾值而非新事件起始值，防止跳变点产生误差
                var (endX, endY) = AbsPosOut(iEnd);
                var segStart = iStart;
                var (segX, segY) = AbsPosIn(iStart);

                for (var cur = iStart; cur < iEnd;)
                {
                    var next = cur + step;
                    if (next > iEnd) next = iEnd;
                    var isLast = next >= iEnd;

                    var (nextX, nextY) = isLast ? (endX, endY) : AbsPosIn(next);
                    var shouldCut = isLast;

                    if (!isLast)
                    {
                        // 将当前段起点到区间终点做线性预测，误差超出容差时切割
                        var segLen = (double)(iEnd - segStart);
                        var progress = segLen > 1e-12 ? (double)(next - segStart) / segLen : 1.0;
                        var predX = segX + (endX - segX) * progress;
                        var predY = segY + (endY - segY) * progress;
                        var thrX = tolerance / 100.0 * ((Math.Abs(segX) + Math.Abs(nextX)) / 2.0 + 1e-9);
                        var thrY = tolerance / 100.0 * ((Math.Abs(segY) + Math.Abs(nextY)) / 2.0 + 1e-9);
                        shouldCut = Math.Abs(nextX - predX) > thrX || Math.Abs(nextY - predY) > thrY;
                    }

                    if (shouldCut)
                    {
                        resultX.Add(new Rpe.Event<float>
                            { StartBeat = segStart, EndBeat = next, StartValue = (float)segX, EndValue = (float)nextX });
                        resultY.Add(new Rpe.Event<float>
                            { StartBeat = segStart, EndBeat = next, StartValue = (float)segY, EndValue = (float)nextY });
                        segStart = next; segX = nextX; segY = nextY;
                    }

                    cur = next;
                }
            }

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventProcessor.EventMergePlus(a, b));

            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // 共享辅助方法

    /// <summary>
    /// 按层顺序将某一通道的事件列表叠加合并。
    /// 层间叠加不满足交换律，同一通道内的层必须串行顺序处理；不同通道间互不依赖，可并行。
    /// </summary>
    internal static List<Rpe.Event<float>> MergeLayerChannel(
        List<Rpe.EventLayer> layers,
        Func<Rpe.EventLayer, List<Rpe.Event<float>>?> selector,
        Func<List<Rpe.Event<float>>, List<Rpe.Event<float>>, List<Rpe.Event<float>>> merge)
    {
        var result = new List<Rpe.Event<float>>();
        foreach (var layer in layers)
        {
            var ch = selector(layer);
            if (ch is { Count: > 0 })
                result = merge(result, ch);
        }
        return result;
    }

    /// <summary>
    /// 获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。
    /// 列表为空时返回 (Beat(0), Beat(0))。
    /// </summary>
    internal static (Beat min, Beat max) GetEventRange(List<Rpe.Event<float>> events)
    {
        if (events.Count == 0) return (new Beat(0), new Beat(0));
        return (events.Min(e => e.StartBeat)!, events.Max(e => e.EndBeat)!);
    }

    /// <summary>
    /// 将计算结果写回判定线：
    /// 清除第 1 层及以上的 X/Y 事件，将压缩后的结果写入第 0 层，
    /// 并在 RotateWithFather 为 true 时将父线旋转事件叠加进来。
    /// 最后将 Father 置为 -1，完成解绑。
    /// </summary>
    internal static void WriteResultToLine(
        Rpe.JudgeLine line,
        List<Rpe.Event<float>> newXEvents,
        List<Rpe.Event<float>> newYEvents,
        List<Rpe.Event<float>> fatherRotateEvents,
        double tolerance,
        Func<List<Rpe.Event<float>>, List<Rpe.Event<float>>, List<Rpe.Event<float>>> merge)
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents.Clear();
            line.EventLayers[i].MoveYEvents.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new Rpe.EventLayer());

        line.EventLayers[0].MoveXEvents = EventProcessor.EventListCompress(newXEvents, tolerance);
        line.EventLayers[0].MoveYEvents = EventProcessor.EventListCompress(newYEvents, tolerance);

        if (line.RotateWithFather)
            line.EventLayers[0].RotateEvents = EventProcessor.EventListCompress(
                merge(line.EventLayers[0].RotateEvents, fatherRotateEvents), tolerance);

        line.Father = -1;
    }
}
