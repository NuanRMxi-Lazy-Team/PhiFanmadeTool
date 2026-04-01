using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、采样算法、结果写回。
/// 同步处理器（<see cref="FatherUnbindProcessor"/>）与异步处理器（<see cref="FatherUnbindAsyncProcessor"/>）共享此类。
/// </summary>
internal static class FatherUnbindHelpers
{
    private static readonly AsyncLocal<CoordinateProfile?> RenderProfileContext = new();

    internal static CoordinateProfile CurrentRenderProfile
        => RenderProfileContext.Value ?? CoordinateProfile.DefaultRenderProfile;

    internal static IDisposable UseRenderProfile(CoordinateProfile renderProfile)
        => new RenderProfileScope(renderProfile);

    private sealed class RenderProfileScope : IDisposable
    {
        private readonly CoordinateProfile? _previous;

        public RenderProfileScope(CoordinateProfile nextProfile)
        {
            _previous = RenderProfileContext.Value;
            RenderProfileContext.Value = nextProfile;
        }

        public void Dispose() => RenderProfileContext.Value = _previous;
    }

    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存：
    /// 同一谱面的所有解绑调用共享同一份缓存，allJudgeLines 被 GC 后自动释放。
    /// </summary>
    internal static readonly ConditionalWeakTable<List<Nrc.JudgeLine>, ConcurrentDictionary<int, Nrc.JudgeLine>>
        ChartCacheTable = new();

    /// <summary>
    /// 根据父线绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    internal static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => CoordinateGeometry.GetNrcAbsolutePos(
            fatherLineX, fatherLineY, angleDegrees, lineX, lineY, CurrentRenderProfile);

    /// <summary>
    /// NRC 虽然以归一化坐标存储，但几何误差必须在当前渲染坐标系评估，
    /// 否则 X/Y 轴缩放不一致会导致切段阈值偏斜。
    /// </summary>
    internal static bool NeedsAdaptiveCut(
        (double X, double Y) segmentStart,
        (double X, double Y) next,
        (double X, double Y) intervalEnd,
        Beat segmentStartBeat,
        Beat intervalEndBeat,
        Beat nextBeat,
        double tolerance)
    {
        var segmentLength = (double)(intervalEndBeat - segmentStartBeat);
        var progress = segmentLength > 1e-12
            ? (double)(nextBeat - segmentStartBeat) / segmentLength
            : 1.0;
        var predicted = (
            X: segmentStart.X + (intervalEnd.X - segmentStart.X) * progress,
            Y: segmentStart.Y + (intervalEnd.Y - segmentStart.Y) * progress);
        var error = CoordinateGeometry.GetNrcScreenDistance(next, predicted, CurrentRenderProfile);
        var threshold = tolerance / 100.0 *
                        ((CoordinateGeometry.GetNrcScreenMagnitude(segmentStart, CurrentRenderProfile) +
                          CoordinateGeometry.GetNrcScreenMagnitude(next, CurrentRenderProfile)) / 2.0 + 1e-9);
        return error > threshold;
    }

    /// <summary>
    /// 传入语义：取 beat 时刻正在生效的事件插值（用于段起点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValIn(List<Nrc.Event<double>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 传出语义：取 beat 时刻即将结束的事件插值（用于段终点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValOut(List<Nrc.Event<double>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat < beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat >= beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 按层顺序将某一通道的事件列表串行叠加合并。层间叠加不满足交换律，必须顺序处理。
    /// </summary>
    internal static List<Nrc.Event<double>> MergeLayerChannel(
        List<Nrc.EventLayer> layers,
        Func<Nrc.EventLayer, List<Nrc.Event<double>>?> selector,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge)
    {
        var result = new List<Nrc.Event<double>>();
        return layers.Select(selector)
            .Where(ch => ch is { Count: > 0 })
            .Select(ch => ch!)
            .Aggregate(result, (current, ch) => merge(current, ch));
    }

    /// <summary>
    /// 命中缓存时返回克隆结果，避免调用方直接持有缓存实例。
    /// </summary>
    internal static bool TryGetCachedClone(
        int targetJudgeLineIndex,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        string logTag,
        out Nrc.JudgeLine cachedClone)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"{logTag}[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            cachedClone = cached.Clone();
            return true;
        }

        cachedClone = null;
        return false;
    }

    /// <summary>
    /// 判定线无父线时直接缓存并返回，统一同步/异步处理器的短路分支。
    /// </summary>
    internal static bool TryReturnWhenNoFather(
        int targetJudgeLineIndex,
        Nrc.JudgeLine judgeLineCopy,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        string logTag)
    {
        if (judgeLineCopy.Father > -1) return false;
        NrcToolLog.OnWarning($"{logTag}[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
        cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
        return true;
    }

    /// <summary>
    /// 清理判定线与父线的全零事件层，减少后续通道合并计算量。
    /// </summary>
    internal static void CleanupRedundantLayers(Nrc.JudgeLine judgeLineCopy, Nrc.JudgeLine fatherLineCopy)
    {
        judgeLineCopy.EventLayers =
            LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
        fatherLineCopy.EventLayers =
            LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;
    }

    /// <summary>获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。列表为空时返回 (0, 0)。</summary>
    internal static (Beat Min, Beat Max) GetEventRange(List<Nrc.Event<double>> events)
        => events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat) ?? new Beat(0), events.Max(e => e.EndBeat) ?? new Beat(0));

    /// <summary>
    /// 将计算结果写回判定线：清除第 1 层及以上的 X/Y 事件，将压缩后的结果写入第 0 层。
    /// RotateWithFather 为 true 时叠加父线旋转事件；最后置 Father = -1 完成解绑。
    /// </summary>
    internal static void WriteResultToLine(
        Nrc.JudgeLine line,
        List<Nrc.Event<double>> newXEvents,
        List<Nrc.Event<double>> newYEvents,
        List<Nrc.Event<double>> fatherRotateEvents,
        double tolerance,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge,
        bool compress = true)
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents.Clear();
            line.EventLayers[i].MoveYEvents.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new Nrc.EventLayer());

        line.EventLayers[0].MoveXEvents = compress
            ? EventCompressor.EventListCompress(newXEvents, tolerance)
            : newXEvents;
        line.EventLayers[0].MoveYEvents = compress
            ? EventCompressor.EventListCompress(newYEvents, tolerance)
            : newYEvents;

        if (line.RotateWithFather)
        {
            var merged = merge(line.EventLayers[0].RotateEvents, fatherRotateEvents);
            line.EventLayers[0].RotateEvents = compress
                ? EventCompressor.EventListCompress(merged, tolerance)
                : merged;
        }

        line.Father = -1;
    }

    // ─── 共享数据结构 ────────────────────────────────────────────────────────

    /// <summary>
    /// 封装解绑计算所需的五个事件通道：父线 X/Y/旋转 和子线 X/Y。
    /// 使用 readonly record struct 保证值语义，可安全在多线程闭包中捕获。
    /// </summary>
    internal readonly record struct EventChannels(
        List<Nrc.Event<double>> Fx,
        List<Nrc.Event<double>> Fy,
        List<Nrc.Event<double>> Fr,
        List<Nrc.Event<double>> Tx,
        List<Nrc.Event<double>> Ty);

    /// <summary>
    /// 合并父子线五个通道事件，统一 EventChannels 拼装顺序。
    /// </summary>
    internal static EventChannels MergeChannels(
        List<Nrc.EventLayer> targetLayers,
        List<Nrc.EventLayer> fatherLayers,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge)
        => new(
            Fx: MergeLayerChannel(fatherLayers, l => l.MoveXEvents, merge),
            Fy: MergeLayerChannel(fatherLayers, l => l.MoveYEvents, merge),
            Fr: MergeLayerChannel(fatherLayers, l => l.RotateEvents, merge),
            Tx: MergeLayerChannel(targetLayers, l => l.MoveXEvents, merge),
            Ty: MergeLayerChannel(targetLayers, l => l.MoveYEvents, merge));

    /// <summary>
    /// 异步并行合并父子线五个通道事件。
    /// </summary>
    internal static async Task<EventChannels> MergeChannelsAsync(
        List<Nrc.EventLayer> targetLayers,
        List<Nrc.EventLayer> fatherLayers,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge)
    {
        var mergeResults = await Task.WhenAll(
            Task.Run(() => MergeLayerChannel(targetLayers, l => l.MoveXEvents, merge)),
            Task.Run(() => MergeLayerChannel(targetLayers, l => l.MoveYEvents, merge)),
            Task.Run(() => MergeLayerChannel(fatherLayers, l => l.MoveXEvents, merge)),
            Task.Run(() => MergeLayerChannel(fatherLayers, l => l.MoveYEvents, merge)),
            Task.Run(() => MergeLayerChannel(fatherLayers, l => l.RotateEvents, merge))
        );

        return new EventChannels(
            Fx: mergeResults[2],
            Fy: mergeResults[3],
            Fr: mergeResults[4],
            Tx: mergeResults[0],
            Ty: mergeResults[1]);
    }

    // ─── 等间隔采样算法 ──────────────────────────────────────────────────────

    /// <summary>
    /// 生成从 <paramref name="min"/> 到 <paramref name="max"/>（不含）以 <paramref name="step"/> 为步长的拍列表。
    /// </summary>
    internal static List<Beat> BuildBeatList(Beat min, Beat max, Beat step)
    {
        var beats = new List<Beat>();
        for (var b = min; b < max; b += step) beats.Add(b);
        return beats;
    }

    /// <summary>
    /// 并行等间隔采样：对 <paramref name="beats"/> 中每一段计算绝对坐标，返回按顺序排列的 X/Y 事件列表。
    /// </summary>
    internal static (List<Nrc.Event<double>> x, List<Nrc.Event<double>> y) EqualSpacingSampling(
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

    /// <summary>
    /// 计算单个采样段 [<paramref name="beat"/>, <paramref name="next"/>] 的 X/Y 绝对坐标事件。
    /// 段起点取 GetValIn（正在生效的插值），段终点取 GetValOut（即将结束的插值）。
    /// </summary>
    internal static (Nrc.Event<double> x, Nrc.Event<double> y) ComputeBeatSegment(
        Beat beat, Beat next, EventChannels ch)
    {
        var (startAbsX, startAbsY) = GetLinePos(
            GetValIn(ch.Fx, beat), GetValIn(ch.Fy, beat), GetValIn(ch.Fr, beat),
            GetValIn(ch.Tx, beat), GetValIn(ch.Ty, beat));
        var (endAbsX, endAbsY) = GetLinePos(
            GetValOut(ch.Fx, next), GetValOut(ch.Fy, next), GetValOut(ch.Fr, next),
            GetValOut(ch.Tx, next), GetValOut(ch.Ty, next));

        return (
            new Nrc.Event<double> { StartBeat = beat, EndBeat = next, StartValue = startAbsX, EndValue = endAbsX },
            new Nrc.Event<double> { StartBeat = beat, EndBeat = next, StartValue = startAbsY, EndValue = endAbsY }
        );
    }

    // ─── 自适应采样算法 ──────────────────────────────────────────────────────

    /// <summary>
    /// 尝试计算五个通道的总体拍范围。若所有通道均为空则返回 <see langword="null"/>。
    /// </summary>
    internal static (Beat min, Beat max)? TryGetOverallRange(EventChannels ch)
    {
        Beat overallMin = new(0), overallMax = new(0);
        var hasEvents = false;
        foreach (var list in new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr })
        {
            if (list.Count == 0) continue;
            var (mn, mx) = GetEventRange(list);
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

    /// <summary>
    /// 收集所有通道事件的起止拍作为关键帧，在 [<paramref name="overallMin"/>, <paramref name="overallMax"/>]
    /// 范围内去重排序后返回。关键帧是自适应采样的强制切割点。
    /// </summary>
    internal static List<Beat> CollectKeyBeats(Beat overallMin, Beat overallMax, EventChannels ch)
    {
        var keyBeatsList = new List<Beat> { overallMin, overallMax };
        foreach (var list in new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr })
        {
            foreach (var e in list)
            {
                if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
            }
        }

        return keyBeatsList.Distinct().OrderBy(b => b).ToList();
    }

    /// <summary>
    /// 并行自适应采样：对 <paramref name="keyBeats"/> 中的每个区间调用
    /// <see cref="AdaptiveSampleInterval"/>，汇总后返回 X/Y 事件列表。
    /// </summary>
    internal static (List<Nrc.Event<double>> x, List<Nrc.Event<double>> y) RunAdaptiveSampling(
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

        // 捕获 EventChannels 到局部函数，避免闭包捕获可变变量
        (double X, double Y) AbsPosIn(Beat b) => GetLinePos(
            GetValIn(ch.Fx, b), GetValIn(ch.Fy, b), GetValIn(ch.Fr, b),
            GetValIn(ch.Tx, b), GetValIn(ch.Ty, b));

        (double X, double Y) AbsPosOut(Beat b) => GetLinePos(
            GetValOut(ch.Fx, b), GetValOut(ch.Fy, b), GetValOut(ch.Fr, b),
            GetValOut(ch.Tx, b), GetValOut(ch.Ty, b));

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
    }

    /// <summary>
    /// 对单个区间 [<paramref name="iStart"/>, <paramref name="iEnd"/>] 进行自适应分段采样：
    /// 以 <paramref name="step"/> 推进，当相邻采样点误差超出容差时插入切割点，否则延续当前段。
    /// </summary>
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
}