using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑同步处理器。
/// </summary>
internal static class FatherUnbindProcessor
{
    /// <summary>
    /// 等间隔采样解绑：将判定线与父线解绑并保持行为一致。
    /// </summary>
    internal static Nrc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        bool compress = true)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbind[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo($"FatherUnbind[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = FatherUnbind(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var tX = FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));
            var tY = FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));
            var fX = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));
            var fY = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));
            var fR = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));

            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(tX);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(tY);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(fX);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(fY);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(fR);

            var cutLength = new Beat(1d / precision);
            var cutTasks = new[]
            {
                Task.Run(() => EventCutter.CutEventsInRange(tX, txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(tY, tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(fX, fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(fY, fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(fR, frMin, frMax, cutLength))
            };
            Task.WaitAll(cutTasks);

            tX = cutTasks[0].Result;
            tY = cutTasks[1].Result;
            fX = cutTasks[2].Result;
            fY = cutTasks[3].Result;
            fR = cutTasks[4].Result;

            var overallMin = new Beat(Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin));
            var overallMax = new Beat(Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax));
            var step = new Beat(1d / precision);

            var beats = new List<Beat>();
            for (var b = overallMin; b < overallMax; b += step)
                beats.Add(b);

            NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            var xBag = new ConcurrentBag<(int i, Nrc.Event<double> evt)>();
            var yBag = new ConcurrentBag<(int i, Nrc.Event<double> evt)>();

            var capTx = tX;
            var capTy = tY;
            var capFx = fX;
            var capFy = fY;
            var capFr = fR;

            Parallel.For(0, beats.Count, i =>
            {
                var beat = beats[i];
                var next = beat + step > overallMax ? overallMax : beat + step;

                var (startAbsX, startAbsY) = FatherUnbindHelpers.GetLinePos(
                    FatherUnbindHelpers.GetValIn(capFx, beat), FatherUnbindHelpers.GetValIn(capFy, beat),
                    FatherUnbindHelpers.GetValIn(capFr, beat),
                    FatherUnbindHelpers.GetValIn(capTx, beat), FatherUnbindHelpers.GetValIn(capTy, beat));
                var (endAbsX, endAbsY) = FatherUnbindHelpers.GetLinePos(
                    FatherUnbindHelpers.GetValOut(capFx, next), FatherUnbindHelpers.GetValOut(capFy, next),
                    FatherUnbindHelpers.GetValOut(capFr, next),
                    FatherUnbindHelpers.GetValOut(capTx, next), FatherUnbindHelpers.GetValOut(capTy, next));

                xBag.Add((i, new Nrc.Event<double>
                    { StartBeat = beat, EndBeat = next, StartValue = (double)startAbsX, EndValue = (double)endAbsX }));
                yBag.Add((i, new Nrc.Event<double>
                    { StartBeat = beat, EndBeat = next, StartValue = (double)startAbsY, EndValue = (double)endAbsY }));
            });

            var sortedX = xBag.OrderBy(x => x.i).Select(x => x.evt).ToList();
            var sortedY = yBag.OrderBy(x => x.i).Select(x => x.evt).ToList();

            NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, fR, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbind[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbind[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbind[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 自适应采样解绑：以事件边界为强制切割点，仅在误差超过容差时插入新采样段。
    /// </summary>
    internal static Nrc.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbindPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo($"FatherUnbindPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug($"FatherUnbindPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = FatherUnbindPlus(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var tX = FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));
            var tY = FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));
            var fX = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));
            var fY = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));
            var fR = FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));

            Beat overallMin = new(0), overallMax = new(0);
            var hasEvents = false;
            foreach (var list in new[] { tX, tY, fX, fY, fR })
            {
                if (list.Count == 0) continue;
                var (mn, mx) = FatherUnbindHelpers.GetEventRange(list);
                if (!hasEvents)
                {
                    overallMin = mn;
                    overallMax = mx;
                    hasEvents = true;
                }
                else
                {
                    if (mn < overallMin) overallMin = mn;
                    if (mx > overallMax) overallMax = mx;
                }
            }

            if (!hasEvents)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var step = new Beat(1d / precision);

            var keyBeatsList = new List<Beat> { overallMin, overallMax };
            foreach (var list in new[] { tX, tY, fX, fY, fR })
            foreach (var e in list)
            {
                if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
            }

            var keyBeats = keyBeatsList.Distinct().OrderBy(b => b).ToList();

            NrcToolLog.OnDebug(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            var resultX = new List<Nrc.Event<double>>();
            var resultY = new List<Nrc.Event<double>>();
            var segmentCount = keyBeats.Count - 1;
            var segmentsX = new List<Nrc.Event<double>>[segmentCount];
            var segmentsY = new List<Nrc.Event<double>>[segmentCount];
            for (var i = 0; i < segmentCount; i++)
            {
                segmentsX[i] = [];
                segmentsY[i] = [];
            }

            var capTx = tX;
            var capTy = tY;
            var capFx = fX;
            var capFy = fY;
            var capFr = fR;

            Parallel.For(0, segmentCount, ki =>
            {
                var iStart = keyBeats[ki];
                var iEnd = keyBeats[ki + 1];
                if (iStart >= iEnd) return;

                var localX = segmentsX[ki];
                var localY = segmentsY[ki];

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
                        shouldCut = FatherUnbindHelpers.NeedsAdaptiveCut(
                            (segX, segY), (nextX, nextY), (endX, endY),
                            segStart, iEnd, next, tolerance);
                    }

                    if (shouldCut)
                    {
                        localX.Add(new Nrc.Event<double>
                        {
                            StartBeat = segStart, EndBeat = next, StartValue = (double)segX, EndValue = (double)nextX
                        });
                        localY.Add(new Nrc.Event<double>
                        {
                            StartBeat = segStart, EndBeat = next, StartValue = (double)segY, EndValue = (double)nextY
                        });
                        segStart = next;
                        segX = nextX;
                        segY = nextY;
                    }

                    cur = next;
                }
            });

            foreach (var seg in segmentsX) resultX.AddRange(seg);
            foreach (var seg in segmentsY) resultY.AddRange(seg);

            NrcToolLog.OnDebug($"FatherUnbindPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;

            (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindHelpers.GetLinePos(
                FatherUnbindHelpers.GetValIn(capFx, beat), FatherUnbindHelpers.GetValIn(capFy, beat),
                FatherUnbindHelpers.GetValIn(capFr, beat),
                FatherUnbindHelpers.GetValIn(capTx, beat), FatherUnbindHelpers.GetValIn(capTy, beat));

            (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindHelpers.GetLinePos(
                FatherUnbindHelpers.GetValOut(capFx, beat), FatherUnbindHelpers.GetValOut(capFy, beat),
                FatherUnbindHelpers.GetValOut(capFr, beat),
                FatherUnbindHelpers.GetValOut(capTx, beat), FatherUnbindHelpers.GetValOut(capTy, beat));
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbindPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}