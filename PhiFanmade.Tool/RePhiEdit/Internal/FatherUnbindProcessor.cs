using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

/// <summary>
/// 判定线父子关系处理器
/// </summary>
internal static class FatherUnbindProcessor
{
    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存（同步 / 异步处理器共用此表）：
    /// - 同一谱面（同一 List 引用）的所有解绑调用共享同一份缓存，避免重复解绑同一父线。
    /// - 不同谱面（不同 List 引用）各自独立，不会相互污染。
    /// - allJudgeLines 被 GC 后对应缓存自动释放，无需手动清理。
    /// - 使用 ConcurrentDictionary 保证同步/异步混用、多线程并发调用时的线程安全。
    /// </summary>
    internal static readonly ConditionalWeakTable<List<Rpe.JudgeLine>, ConcurrentDictionary<int, Rpe.JudgeLine>>
        ChartCacheTable = new();

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
    /// 同一 <paramref name="allJudgeLines"/> 实例内的多次调用会共享解绑缓存，避免重复解绑同一父线；
    /// 不同谱面（不同 List 实例）的缓存自动隔离，无需手动清理。
    /// </summary>
    public static Rpe.JudgeLine FatherUnbind(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d) =>
        FatherUnbindCore(targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            ChartCacheTable.GetOrCreateValue(allJudgeLines));

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。（自适应采样节省性能版本）
    /// 同一 <paramref name="allJudgeLines"/> 实例内的多次调用会共享解绑缓存，避免重复解绑同一父线；
    /// 不同谱面（不同 List 实例）的缓存自动隔离，无需手动清理。
    /// </summary>
    public static Rpe.JudgeLine FatherUnbindPlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d) =>
        FatherUnbindCorePlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            ChartCacheTable.GetOrCreateValue(allJudgeLines));

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// 策略：等间隔采样（精度由 precision 决定），多线程并行计算各采样段的绝对坐标。
    /// </summary>
    private static Rpe.JudgeLine FatherUnbindCore(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision, double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache)
    {
        // ── 缓存命中：该线已被解绑过，直接返回副本，避免重复计算 ──
        if (cache.TryGetValue(targetJudgeLineIndex, out var cachedResult))
        {
            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cachedResult.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbind[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                // 传入共享缓存：若父线已被其他子线解绑过，直接复用缓存结果
                fatherLineCopy = FatherUnbindCore(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
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
            var capTx = tX;
            var capTy = tY;
            var capFx = fX;
            var capFy = fY;
            var capFr = fR;
            var cutTasks = new[]
            {
                Task.Run(() => EventProcessor.CutEventsInRange(capTx, txMin, txMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(capTy, tyMin, tyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(capFx, fxMin, fxMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(capFy, fyMin, fyMax)),
                Task.Run(() => EventProcessor.CutEventsInRange(capFr, frMin, frMax))
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
                var prevFx = i > 0 ? fX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevFy = i > 0 ? fY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevFr = i > 0 ? fR.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevTx = i > 0 ? tX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                var prevTy = i > 0 ? tY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;

                var fxEvt = fX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var fyEvt = fY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var frEvt = fR.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var txEvt = tX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                var tyEvt = tY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);

                var (startAbsX, startAbsY) = GetLinePos(
                    fxEvt?.StartValue ?? prevFx, fyEvt?.StartValue ?? prevFy, frEvt?.StartValue ?? prevFr,
                    txEvt?.StartValue ?? prevTx, tyEvt?.StartValue ?? prevTy);
                var (endAbsX, endAbsY) = GetLinePos(
                    fxEvt?.EndValue ?? prevFx, fyEvt?.EndValue ?? prevFy, frEvt?.EndValue ?? prevFr,
                    txEvt?.EndValue ?? prevTx, tyEvt?.EndValue ?? prevTy);

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

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
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
    private static Rpe.JudgeLine FatherUnbindCorePlus(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision, double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache)
    {
        // 缓存命中：该线已被解绑过，直接返回副本，避免重复计算
        if (cache.TryGetValue(targetJudgeLineIndex, out var cachedResult))
        {
            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cachedResult.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                // 传入共享缓存：若父线已被其他子线解绑过，直接复用缓存结果
                fatherLineCopy =
                    FatherUnbindCorePlus(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，可并行）
            var tX = MergeLayerChannel(tLayers, l => l.MoveXEvents, (a, b) =>
                EventProcessor.EventMergePlus(a, b));
            var tY = MergeLayerChannel(tLayers, l => l.MoveYEvents, (a, b) =>
                EventProcessor.EventMergePlus(a, b));
            var fX = MergeLayerChannel(fLayers, l => l.MoveXEvents, (a, b) =>
                EventProcessor.EventMergePlus(a, b));
            var fY = MergeLayerChannel(fLayers, l => l.MoveYEvents, (a, b) =>
                EventProcessor.EventMergePlus(a, b));
            var fR = MergeLayerChannel(fLayers, l => l.RotateEvents, (a, b) =>
                EventProcessor.EventMergePlus(a, b));

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            Beat overallMin = new(0), overallMax = new(0);
            var hasEvents = false;
            foreach (var list in new[] { tX, tY, fX, fY })
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

            if (!hasEvents)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
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

            // 各 key interval 完全独立，Parallel.For 并行处理
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
                            StartBeat = segStart, EndBeat = next, StartValue = (float)segX, EndValue = (float)nextX
                        });
                        localY.Add(new Rpe.Event<float>
                        {
                            StartBeat = segStart, EndBeat = next, StartValue = (float)segY, EndValue = (float)nextY
                        });
                        segStart = next;
                        segX = nextX;
                        segY = nextY;
                    }

                    cur = next;
                }
            });

            // 各段按 key interval 顺序合并，保证事件时序正确
            foreach (var seg in segmentsX) resultX.AddRange(seg);
            foreach (var seg in segmentsY) resultY.AddRange(seg);

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventProcessor.EventMergePlus(a, b));

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;

            // AbsPosIn/AbsPosOut 捕获通道变量，保留为本地函数；
            // GetValIn/GetValOut 已提升为静态方法，O(log n) 二分查找。
            (double X, double Y) AbsPosIn(Beat beat) => GetLinePos(
                GetValIn(fX, beat), GetValIn(fY, beat), GetValIn(fR, beat),
                GetValIn(tX, beat), GetValIn(tY, beat));

            (double X, double Y) AbsPosOut(Beat beat) => GetLinePos(
                GetValOut(fX, beat), GetValOut(fY, beat), GetValOut(fR, beat),
                GetValOut(tX, beat), GetValOut(tY, beat));
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
    /// 传入语义：取 beat 时刻正在生效的事件插值（用于段起点）。
    /// 要求事件列表已按 StartBeat 升序排列；二分查找，O(log n)。
    /// </summary>
    internal static float GetValIn(List<Rpe.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        // 找最后一个 StartBeat ≤ beat 的事件
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
        // EndBeat > beat：事件尚在进行，插值；否则取末尾值
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 传出语义：取 beat 时刻即将结束的事件插值（用于段终点，避免取新事件 StartValue 造成虚假连贯）。
    /// 要求事件列表已按 StartBeat 升序排列；二分查找，O(log n)。
    /// </summary>
    internal static float GetValOut(List<Rpe.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        // 找最后一个 StartBeat < beat 的事件
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
    /// 按层顺序将某一个通道的事件列表叠加合并。
    /// 层间叠加不满足交换律，同一通道内的层必须串行顺序处理；不同通道间互不依赖，可并行。
    /// </summary>
    internal static List<Rpe.Event<float>> MergeLayerChannel(
        List<Rpe.EventLayer> layers,
        Func<Rpe.EventLayer, List<Rpe.Event<float>>?> selector,
        Func<List<Rpe.Event<float>>, List<Rpe.Event<float>>, List<Rpe.Event<float>>> merge)
    {
        var result = new List<Rpe.Event<float>>();
        foreach (var ch in layers.Select(selector))
        {
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
        return events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat), events.Max(e => e.EndBeat));
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
        // 确保这条线的第一层级每个事件列表都至少存在一个垫底事件，且列表必须存在，这是兼容一些不遵守空列表行为的模拟器所使用的，后续删掉也无可厚非
        line.EventLayers[0].AlphaEvents ??= [];
        if (line.EventLayers[0].AlphaEvents.Count == 0)
            line.EventLayers[0].AlphaEvents.Add(new Rpe.Event<int>
                { StartBeat = new Beat(0), EndBeat = new Beat(1), StartValue = 0, EndValue = 0 });
        line.EventLayers[0].SpeedEvents ??= [];
        if (line.EventLayers[0].SpeedEvents.Count == 0)
            line.EventLayers[0].SpeedEvents.Add(new Rpe.Event<float>
                { StartBeat = new Beat(0), EndBeat = new Beat(1), StartValue = 0, EndValue = 0 });
        line.Father = -1;
    }
}