using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.RePhiEdit.Events.Internal;
using PhiFanmade.Tool.RePhiEdit.Layers.Internal;

namespace PhiFanmade.Tool.RePhiEdit.JudgeLines.Internal;

/// <summary>
/// RPE 判定线父子解绑同步处理器。
/// </summary>
internal static class FatherUnbindProcessor
{
    internal static Rpe.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache,
        bool compress = true)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            RpeToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RpeToolLog.OnWarning($"FatherUnbind[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            RpeToolLog.OnInfo($"FatherUnbind[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RpeToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = FatherUnbind(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache,
                    compress);
            }

            judgeLineCopy.EventLayers = LayerProcessor.RemoveUselessLayer(judgeLineCopy.EventLayers) ??
                                        judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUselessLayer(fatherLineCopy.EventLayers) ??
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
            var capTx = tX;
            var capTy = tY;
            var capFx = fX;
            var capFy = fY;
            var capFr = fR;
            var cutTasks = new Task<List<Rpe.Event<float>>>[]
            {
                Task.Run(() => EventCutter.CutEventsInRange(capTx, txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(capTy, tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(capFx, fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(capFy, fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(capFr, frMin, frMax, cutLength))
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

            RpeToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            var xBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();
            var yBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();

            Parallel.For(0, beats.Count, i =>
            {
                var beat = beats[i];
                var next = beat + step > overallMax ? overallMax : beat + step;

                var (startAbsX, startAbsY) = FatherUnbindHelpers.GetLinePos(
                    FatherUnbindHelpers.GetValIn(fX, beat), FatherUnbindHelpers.GetValIn(fY, beat),
                    FatherUnbindHelpers.GetValIn(fR, beat),
                    FatherUnbindHelpers.GetValIn(tX, beat), FatherUnbindHelpers.GetValIn(tY, beat));
                var (endAbsX, endAbsY) = FatherUnbindHelpers.GetLinePos(
                    FatherUnbindHelpers.GetValOut(fX, next), FatherUnbindHelpers.GetValOut(fY, next),
                    FatherUnbindHelpers.GetValOut(fR, next),
                    FatherUnbindHelpers.GetValOut(tX, next), FatherUnbindHelpers.GetValOut(tY, next));

                xBag.Add((i, new Rpe.Event<float>
                {
                    StartBeat = beat,
                    EndBeat = next,
                    StartValue = (float)startAbsX,
                    EndValue = (float)endAbsX
                }));
                yBag.Add((i, new Rpe.Event<float>
                {
                    StartBeat = beat,
                    EndBeat = next,
                    StartValue = (float)startAbsY,
                    EndValue = (float)endAbsY
                }));
            });

            var sortedX = xBag.OrderBy(x => x.i).Select(x => x.evt).ToList();
            var sortedY = yBag.OrderBy(x => x.i).Select(x => x.evt).ToList();

            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, fR, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            RpeToolLog.OnInfo($"FatherUnbind[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RpeToolLog.OnError($"FatherUnbind[{targetJudgeLineIndex}]: 未知错误: {ex.Message}");
            return judgeLineCopy;
        }
    }

    internal static Rpe.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            RpeToolLog.OnDebug($"FatherUnbindPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RpeToolLog.OnWarning($"FatherUnbindPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                fatherLineCopy = FatherUnbindPlus(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers = LayerProcessor.RemoveUselessLayer(judgeLineCopy.EventLayers) ??
                                        judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUselessLayer(fatherLineCopy.EventLayers) ??
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
                if (list.Count == 0)
                    continue;

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
            {
                foreach (var e in list)
                {
                    if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                    if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
                }
            }

            var keyBeats = keyBeatsList.Distinct().OrderBy(b => b).ToList();
            var resultX = new List<Rpe.Event<float>>();
            var resultY = new List<Rpe.Event<float>>();
            var segmentCount = keyBeats.Count - 1;
            var segmentsX = new List<Rpe.Event<float>>[segmentCount];
            var segmentsY = new List<Rpe.Event<float>>[segmentCount];
            for (var i = 0; i < segmentCount; i++)
            {
                segmentsX[i] = [];
                segmentsY[i] = [];
            }

            Parallel.For(0, segmentCount, ki =>
            {
                var iStart = keyBeats[ki];
                var iEnd = keyBeats[ki + 1];
                if (iStart >= iEnd)
                    return;

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
                        localX.Add(new Rpe.Event<float>
                        {
                            StartBeat = segStart,
                            EndBeat = next,
                            StartValue = (float)segX,
                            EndValue = (float)nextX
                        });
                        localY.Add(new Rpe.Event<float>
                        {
                            StartBeat = segStart,
                            EndBeat = next,
                            StartValue = (float)segY,
                            EndValue = (float)nextY
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

            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            return judgeLineCopy;

            (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindHelpers.GetLinePos(
                FatherUnbindHelpers.GetValIn(fX, beat), FatherUnbindHelpers.GetValIn(fY, beat),
                FatherUnbindHelpers.GetValIn(fR, beat),
                FatherUnbindHelpers.GetValIn(tX, beat), FatherUnbindHelpers.GetValIn(tY, beat));

            (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindHelpers.GetLinePos(
                FatherUnbindHelpers.GetValOut(fX, beat), FatherUnbindHelpers.GetValOut(fY, beat),
                FatherUnbindHelpers.GetValOut(fR, beat),
                FatherUnbindHelpers.GetValOut(tX, beat), FatherUnbindHelpers.GetValOut(tY, beat));
        }
        catch (Exception ex)
        {
            RpeToolLog.OnError($"FatherUnbindPlus[{targetJudgeLineIndex}]: 未知错误: {ex.Message}");
            return judgeLineCopy;
        }
    }
}