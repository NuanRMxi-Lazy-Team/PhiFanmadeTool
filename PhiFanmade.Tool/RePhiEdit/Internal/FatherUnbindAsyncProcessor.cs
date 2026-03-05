using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

/// <summary>
/// 判定线父子关系异步处理器（async/await 版本）
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。（async 等间隔采样版本）
    /// 各通道层级合并并行执行，等间隔主采样循环异步卸载至线程池。
    /// </summary>
    public static async Task<Rpe.JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbindAsync[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindAsync[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindAsync(judgeLineCopy.Father, allJudgeLinesCopy, precision,
                    tolerance);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，并行执行）
            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMerge(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMerge(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMerge(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMerge(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.RotateEvents, (a, b) => EventProcessor.EventMerge(a, b)))
            );

            var tX = mergeResults[0];
            var tY = mergeResults[1];
            var fX = mergeResults[2];
            var fY = mergeResults[3];
            var fR = mergeResults[4];

            var (txMin, txMax) = FatherUnbindProcessor.GetEventRange(tX);
            var (tyMin, tyMax) = FatherUnbindProcessor.GetEventRange(tY);
            var (fxMin, fxMax) = FatherUnbindProcessor.GetEventRange(fX);
            var (fyMin, fyMax) = FatherUnbindProcessor.GetEventRange(fY);
            var (frMin, frMax) = FatherUnbindProcessor.GetEventRange(fR);

            // 5 个通道互不依赖，并行切割为等长小段
            var cutResults = await Task.WhenAll(
                Task.Run(() => EventProcessor.CutEventsInRange(tX, txMin, txMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(tY, tyMin, tyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fX, fxMin, fxMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fY, fyMin, fyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(fR, frMin, frMax))
            );

            tX = cutResults[0]; tY = cutResults[1];
            fX = cutResults[2]; fY = cutResults[3]; fR = cutResults[4];

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            var overallMin = new Beat(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)));
            var overallMax = new Beat(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)));
            var step = new Beat(1d / precision);

            var beats = new List<Beat>();
            for (var b = overallMin; b <= overallMax; b += step)
                beats.Add(b);

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            // CPU 密集型主循环：卸载至线程池，内部用 Parallel.For 多核加速
            var (sortedX, sortedY) = await Task.Run(() =>
            {
                var xBag = new System.Collections.Concurrent.ConcurrentBag<(int i, Rpe.Event<float> evt)>();
                var yBag = new System.Collections.Concurrent.ConcurrentBag<(int i, Rpe.Event<float> evt)>();

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

                    var (startAbsX, startAbsY) = FatherUnbindProcessor.GetLinePos(
                        fxEvt?.StartValue ?? prevFX, fyEvt?.StartValue ?? prevFY, frEvt?.StartValue ?? prevFR,
                        txEvt?.StartValue ?? prevTX, tyEvt?.StartValue ?? prevTY);
                    var (endAbsX, endAbsY) = FatherUnbindProcessor.GetLinePos(
                        fxEvt?.EndValue ?? prevFX, fyEvt?.EndValue ?? prevFY, frEvt?.EndValue ?? prevFR,
                        txEvt?.EndValue ?? prevTX, tyEvt?.EndValue ?? prevTY);

                    xBag.Add((i, new Rpe.Event<float>
                        { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsX, EndValue = (float)endAbsX }));
                    yBag.Add((i, new Rpe.Event<float>
                        { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsY, EndValue = (float)endAbsY }));
                });

                return (
                    xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
                    yBag.OrderBy(x => x.i).Select(x => x.evt).ToList()
                );
            });

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，压缩并写回");
            FatherUnbindProcessor.WriteResultToLine(judgeLineCopy, sortedX, sortedY, fR, tolerance,
                (a, b) => EventProcessor.EventMerge(a, b));

            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。（async 自适应采样版本）
    /// 各通道层级合并并行执行，自适应采样主循环异步卸载至线程池。
    /// </summary>
    public static async Task<Rpe.JudgeLine> FatherUnbindAsyncPlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindAsyncPlus(judgeLineCopy.Father, allJudgeLinesCopy, precision,
                    tolerance);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，并行执行）
            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.RotateEvents, (a, b) => EventProcessor.EventMergePlus(a, b)))
            );

            var tX = mergeResults[0];
            var tY = mergeResults[1];
            var fX = mergeResults[2];
            var fY = mergeResults[3];
            var fR = mergeResults[4];

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            Beat overallMin = new Beat(0), overallMax = new Beat(0);
            var hasEvents = false;
            foreach (var list in new[] { tX, tY, fX, fY })
            {
                if (list.Count == 0) continue;
                var (mn, mx) = FatherUnbindProcessor.GetEventRange(list);
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
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            // CPU 密集型自适应采样主循环：卸载至线程池
            var (resultX, resultY) = await Task.Run(() =>
            {
                // 传入语义：取在 beat 时刻正在生效的事件的插值（用于区间/段起点）
                float GetValIn(List<Rpe.Event<float>> events, Beat beat)
                {
                    if (events.Count == 0) return 0f;
                    var active = events.Where(e => e.StartBeat <= beat && e.EndBeat > beat)
                        .MaxBy(e => e.StartBeat);
                    if (active != null) return active.GetValueAtBeat(beat);
                    return events.FindLast(e => e.EndBeat <= beat)?.EndValue ?? 0f;
                }

                // 传出语义：取在 beat 时刻即将结束的事件的插值（用于区间/段终点，避免取到新事件 StartValue 造成虚假连贯）
                float GetValOut(List<Rpe.Event<float>> events, Beat beat)
                {
                    if (events.Count == 0) return 0f;
                    var active = events.Where(e => e.StartBeat < beat && e.EndBeat >= beat)
                        .MaxBy(e => e.StartBeat);
                    if (active != null) return active.GetValueAtBeat(beat);
                    return events.FindLast(e => e.EndBeat <= beat)?.EndValue ?? 0f;
                }

                (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindProcessor.GetLinePos(
                    GetValIn(fX, beat), GetValIn(fY, beat), GetValIn(fR, beat),
                    GetValIn(tX, beat), GetValIn(tY, beat));

                (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindProcessor.GetLinePos(
                    GetValOut(fX, beat), GetValOut(fY, beat), GetValOut(fR, beat),
                    GetValOut(tX, beat), GetValOut(tY, beat));

                var resX = new List<Rpe.Event<float>>();
                var resY = new List<Rpe.Event<float>>();

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
                            resX.Add(new Rpe.Event<float>
                                { StartBeat = segStart, EndBeat = next, StartValue = (float)segX, EndValue = (float)nextX });
                            resY.Add(new Rpe.Event<float>
                                { StartBeat = segStart, EndBeat = next, StartValue = (float)segY, EndValue = (float)nextY });
                            segStart = next; segX = nextX; segY = nextY;
                        }

                        cur = next;
                    }
                }

                return (resX, resY);
            });

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindProcessor.WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventProcessor.EventMergePlus(a, b));

            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}
