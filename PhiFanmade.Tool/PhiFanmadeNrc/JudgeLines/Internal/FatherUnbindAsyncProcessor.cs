using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑异步处理器（async/await 版本）。
/// 缓存与同步版 <see cref="FatherUnbindProcessor"/> 共享同一张 <see cref="FatherUnbindHelpers.ChartCacheTable"/>。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    private readonly record struct EventChannels(
        List<Nrc.Event<double>> Fx,
        List<Nrc.Event<double>> Fy,
        List<Nrc.Event<double>> Fr,
        List<Nrc.Event<double>> Tx,
        List<Nrc.Event<double>> Ty);

    // 等间隔采样异步版

    internal static async Task<Nrc.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        bool compress)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindAsync[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache, compress);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress)))
            );

            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergeResults[0]);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[1]);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergeResults[2]);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[3]);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergeResults[4]);
            var cutLength = new Beat(1d / precision);

            var cutResults = await Task.WhenAll(
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[0], txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[1], tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[2], fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[3], fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[4], frMin, frMax, cutLength))
            );

            var ch = new EventChannels(cutResults[2], cutResults[3], cutResults[4], cutResults[0], cutResults[1]);
            var overallMin = new Beat(Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin));
            var overallMax = new Beat(Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax));
            var step = new Beat(1d / precision);
            var beats = BuildBeatList(overallMin, overallMax, step);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            var (sortedX, sortedY) = await RunEqualSpacingSamplingAsync(beats, overallMax, step, ch);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // ─── 自适应采样异步版 ────────────────────────────────────────────────────

    internal static async Task<Nrc.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindPlusAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ??
                                         fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance)))
            );

            var rangeResult = TryGetOverallRange(mergeResults[0], mergeResults[1], mergeResults[2], mergeResults[3],
                mergeResults[4]);
            if (rangeResult is null)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var ch = new EventChannels(mergeResults[2], mergeResults[3], mergeResults[4], mergeResults[0],
                mergeResults[1]);
            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);
            var keyBeats = CollectKeyBeats(overallMin, overallMax, new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr });

            NrcToolLog.OnDebug(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            var (resultX, resultY) = await Task.Run(() => RunAdaptiveSampling(keyBeats, step, tolerance, ch));

            NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // 等间隔采样辅助 

    private static List<Beat> BuildBeatList(Beat min, Beat max, Beat step)
    {
        var beats = new List<Beat>();
        for (var b = min; b < max; b += step) beats.Add(b);
        return beats;
    }

    private static Task<(List<Nrc.Event<double>>, List<Nrc.Event<double>>)> RunEqualSpacingSamplingAsync(
        List<Beat> beats, Beat max, Beat step, EventChannels ch)
        => Task.Run(() => EqualSpacingSampling(beats, max, step, ch));

    private static (List<Nrc.Event<double>> x, List<Nrc.Event<double>> y) EqualSpacingSampling(
        List<Beat> beats, Beat max, Beat step, EventChannels ch)
    {
        var xBag = new ConcurrentBag<(int i, Nrc.Event<double> evt)>();
        var yBag = new ConcurrentBag<(int i, Nrc.Event<double> evt)>();

        Parallel.For(0, beats.Count, i =>
        {
            var beat = beats[i];
            var next = beat + step > max ? max : beat + step;
            var (xEvt, yEvt) = ComputeBeatSegment(beat, next, ch);
            xBag.Add((i, xEvt));
            yBag.Add((i, yEvt));
        });

        return (xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
            yBag.OrderBy(x => x.i).Select(x => x.evt).ToList());
    }

    private static (Nrc.Event<double> x, Nrc.Event<double> y) ComputeBeatSegment(
        Beat beat, Beat next, EventChannels ch)
    {
        var (startAbsX, startAbsY) = FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValIn(ch.Fx, beat), FatherUnbindHelpers.GetValIn(ch.Fy, beat),
            FatherUnbindHelpers.GetValIn(ch.Fr, beat),
            FatherUnbindHelpers.GetValIn(ch.Tx, beat), FatherUnbindHelpers.GetValIn(ch.Ty, beat));
        var (endAbsX, endAbsY) = FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValOut(ch.Fx, next), FatherUnbindHelpers.GetValOut(ch.Fy, next),
            FatherUnbindHelpers.GetValOut(ch.Fr, next),
            FatherUnbindHelpers.GetValOut(ch.Tx, next), FatherUnbindHelpers.GetValOut(ch.Ty, next));

        return (
            new Nrc.Event<double>
                { StartBeat = beat, EndBeat = next, StartValue = startAbsX, EndValue = endAbsX },
            new Nrc.Event<double>
                { StartBeat = beat, EndBeat = next, StartValue = startAbsY, EndValue = endAbsY }
        );
    }

    // ─── 自适应采样辅助 

    private static (Beat min, Beat max)? TryGetOverallRange(
        List<Nrc.Event<double>> tX, List<Nrc.Event<double>> tY,
        List<Nrc.Event<double>> fX, List<Nrc.Event<double>> fY,
        List<Nrc.Event<double>> fR)
    {
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

        return hasEvents ? (overallMin, overallMax) : null;
    }

    private static List<Beat> CollectKeyBeats(Beat overallMin, Beat overallMax,
        IEnumerable<List<Nrc.Event<double>>> channels)
    {
        var keyBeatsList = new List<Beat> { overallMin, overallMax };
        foreach (var list in channels)
        foreach (var e in list)
        {
            if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
            if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
        }

        return keyBeatsList.Distinct().OrderBy(b => b).ToList();
    }

    private static (List<Nrc.Event<double>> x, List<Nrc.Event<double>> y) RunAdaptiveSampling(
        List<Beat> keyBeats, Beat step, double tolerance, EventChannels ch)
    {
        var segmentCount = keyBeats.Count - 1;
        var segmentsX = new List<Nrc.Event<double>>[segmentCount];
        var segmentsY = new List<Nrc.Event<double>>[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            segmentsX[i] = [];
            segmentsY[i] = [];
        }

        Parallel.For(0, segmentCount, ki =>
        {
            if (keyBeats[ki] >= keyBeats[ki + 1]) return;
            var (sx, sy) = AdaptiveSampleInterval(
                keyBeats[ki], keyBeats[ki + 1], step, tolerance, AbsPosIn, AbsPosOut);
            segmentsX[ki].AddRange(sx);
            segmentsY[ki].AddRange(sy);
        });

        var resX = new List<Nrc.Event<double>>();
        var resY = new List<Nrc.Event<double>>();
        foreach (var seg in segmentsX) resX.AddRange(seg);
        foreach (var seg in segmentsY) resY.AddRange(seg);
        return (resX, resY);

        (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValOut(ch.Fx, beat), FatherUnbindHelpers.GetValOut(ch.Fy, beat),
            FatherUnbindHelpers.GetValOut(ch.Fr, beat),
            FatherUnbindHelpers.GetValOut(ch.Tx, beat), FatherUnbindHelpers.GetValOut(ch.Ty, beat));

        (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValIn(ch.Fx, beat), FatherUnbindHelpers.GetValIn(ch.Fy, beat),
            FatherUnbindHelpers.GetValIn(ch.Fr, beat),
            FatherUnbindHelpers.GetValIn(ch.Tx, beat), FatherUnbindHelpers.GetValIn(ch.Ty, beat));
    }

    private static (List<Nrc.Event<double>> x, List<Nrc.Event<double>> y) AdaptiveSampleInterval(
        Beat iStart, Beat iEnd, Beat step, double tolerance,
        Func<Beat, (double X, double Y)> absPosIn,
        Func<Beat, (double X, double Y)> absPosOut)
    {
        var localX = new List<Nrc.Event<double>>();
        var localY = new List<Nrc.Event<double>>();

        var end = absPosOut(iEnd);
        var segStart = iStart;
        var seg = absPosIn(iStart);

        for (var cur = iStart; cur < iEnd;)
        {
            var next = cur + step > iEnd ? iEnd : cur + step;
            var isLast = next >= iEnd;
            var nextPos = isLast ? end : absPosIn(next);

            if (isLast || NeedsAdaptiveCut(seg, nextPos, end, segStart, iEnd, next, tolerance))
            {
                localX.Add(new Nrc.Event<double>
                    { StartBeat = segStart, EndBeat = next, StartValue = seg.X, EndValue = nextPos.X });
                localY.Add(new Nrc.Event<double>
                    { StartBeat = segStart, EndBeat = next, StartValue = seg.Y, EndValue = nextPos.Y });
                segStart = next;
                seg = nextPos;
            }

            cur = next;
        }

        return (localX, localY);
    }

    private static bool NeedsAdaptiveCut(
        (double X, double Y) seg, (double X, double Y) next,
        (double X, double Y) end, Beat segStart, Beat iEnd, Beat nextBeat, double tolerance)
        => FatherUnbindHelpers.NeedsAdaptiveCut(seg, next, end, segStart, iEnd, nextBeat, tolerance);
}