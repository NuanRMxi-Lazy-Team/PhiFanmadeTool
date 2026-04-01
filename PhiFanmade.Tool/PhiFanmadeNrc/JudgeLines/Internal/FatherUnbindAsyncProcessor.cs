using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑异步处理器（async/await 版本）。
/// 所有采样算法均委托给 <see cref="FatherUnbindHelpers"/> 中的共享实现，
/// 本类只负责缓存检查、父线递归解绑、通道合并及日志记录。
/// 缓存与同步版 <see cref="FatherUnbindProcessor"/> 共享同一张 <see cref="FatherUnbindHelpers.ChartCacheTable"/>。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    private readonly record struct PrepareResult(
        Nrc.JudgeLine JudgeLine,
        Nrc.JudgeLine? FatherLine,
        bool ShouldReturn);

    // 抽取异步版的前置流程，保证 FatherUnbindAsync / FatherUnbindPlusAsync 的递归与缓存行为一致。
    private static async Task<PrepareResult> PrepareUnbindContextAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        string logTag,
        string startAction,
        Func<int, List<Nrc.JudgeLine>, Task<Nrc.JudgeLine>> recursiveUnbind)
    {
        if (FatherUnbindHelpers.TryGetCachedClone(targetJudgeLineIndex, cache, logTag, out var cached))
        {
            return new PrepareResult(cached, null, true);
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        if (FatherUnbindHelpers.TryReturnWhenNoFather(targetJudgeLineIndex, judgeLineCopy, cache, logTag))
        {
            return new PrepareResult(judgeLineCopy, null, true);
        }

        NrcToolLog.OnInfo($"{logTag}[{targetJudgeLineIndex}]: {startAction}，父线索引={judgeLineCopy.Father}");

        // 若父线仍有父线，递归异步解绑父链，确保父线已为绝对坐标
        var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
        if (fatherLineCopy.Father >= 0)
        {
            NrcToolLog.OnDebug($"{logTag}[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
            fatherLineCopy = await recursiveUnbind(judgeLineCopy.Father, allJudgeLinesCopy);
        }

        FatherUnbindHelpers.CleanupRedundantLayers(judgeLineCopy, fatherLineCopy);

        return new PrepareResult(judgeLineCopy, fatherLineCopy, false);
    }

    // 等间隔采样异步版

    /// <summary>
    /// 等间隔采样解绑（异步版）：将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归异步解绑父链 → 并行合并各通道 → 并行切割 → 异步等间隔采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">误差容差百分比，用于事件压缩。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
    /// <param name="compress">是否在写回前压缩冗余事件。</param>
    internal static async Task<Nrc.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        bool compress)
    {
        Nrc.JudgeLine judgeLineCopy;
        try
        {
            // 统一前置流程，减少等间隔/自适应两个入口的重复实现。
            var (judgeLine, fatherLine, shouldReturn) = await PrepareUnbindContextAsync(
                targetJudgeLineIndex,
                allJudgeLines,
                cache,
                logTag: "FatherUnbindAsync",
                startAction: "开始解绑",
                recursiveUnbind: (idx, lines) => FatherUnbindAsync(idx, lines, precision, tolerance, cache, compress));

            judgeLineCopy = judgeLine;
            
            if (shouldReturn || fatherLine is null)
            {
                return judgeLineCopy;
            }

            // 并行合并各通道事件（等间隔版使用 EventListMerge）；各通道独立，可安全并行
            var mergeChannels = await FatherUnbindHelpers.MergeChannelsAsync(
                judgeLineCopy.EventLayers,
                fatherLine.EventLayers,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress));

            // 将各通道按精度步长并行切割，为等间隔采样准备
            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergeChannels.Tx);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergeChannels.Ty);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergeChannels.Fx);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergeChannels.Fy);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergeChannels.Fr);
            var cutLength = new Beat(1d / precision);

            var cutResults = await Task.WhenAll(
                Task.Run(() => EventCutter.CutEventsInRange(mergeChannels.Tx, txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeChannels.Ty, tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeChannels.Fx, fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeChannels.Fy, fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeChannels.Fr, frMin, frMax, cutLength))
            );

            // 构建事件通道并执行等间隔采样（委托给共享算法）
            var ch = new FatherUnbindHelpers.EventChannels(
                Fx: cutResults[2], Fy: cutResults[3], Fr: cutResults[4],
                Tx: cutResults[0], Ty: cutResults[1]);
            var overallMin = new Beat(Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin));
            var overallMax = new Beat(Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax));
            var step = new Beat(1d / precision);
            var beats = FatherUnbindHelpers.BuildBeatList(overallMin, overallMax, step);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");
            var (sortedX, sortedY) = await Task.Run(
                () => FatherUnbindHelpers.EqualSpacingSampling(beats, overallMax, step, ch));

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // 自适应采样异步版

    /// <summary>
    /// 自适应采样解绑（异步版）：以事件边界为强制切割点，仅在误差超过容差时插入新采样段，
    /// 相较等间隔版可减少冗余段数。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归异步解绑父链 → 并行合并各通道 → 收集关键帧 → 异步自适应采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">自适应采样的最大步数上限（同时作为事件合并精度）。</param>
    /// <param name="tolerance">误差容差百分比，决定何时插入额外切割点及压缩阈值。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
    internal static async Task<Nrc.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache)
    {
        Nrc.JudgeLine judgeLineCopy;
        try
        {
            // 与 FatherUnbindAsync 复用同一前置流程，确保行为对齐。
            var prepare = await PrepareUnbindContextAsync(
                targetJudgeLineIndex,
                allJudgeLines,
                cache,
                logTag: "FatherUnbindAsyncPlus",
                startAction: "开始解绑（自适应采样）",
                recursiveUnbind: (idx, lines) => FatherUnbindPlusAsync(idx, lines, precision, tolerance, cache));

            judgeLineCopy = prepare.JudgeLine;
            if (prepare.ShouldReturn)
            {
                return judgeLineCopy;
            }

            var fatherLine = prepare.FatherLine;
            if (fatherLine is null)
                return judgeLineCopy;

            // 并行合并各通道事件（自适应版使用 EventMergePlus）；各通道独立，可安全并行
            var ch = await FatherUnbindHelpers.MergeChannelsAsync(
                judgeLineCopy.EventLayers,
                fatherLine.EventLayers,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance));

            // 构建事件通道，确定总体拍范围（委托给共享算法）
            var rangeResult = FatherUnbindHelpers.TryGetOverallRange(ch);
            if (rangeResult is null)
            {
                // 所有通道均为空，无需采样，直接解绑
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);
            var keyBeats = FatherUnbindHelpers.CollectKeyBeats(overallMin, overallMax, ch);

            NrcToolLog.OnDebug(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");
            var (resultX, resultY) = await Task.Run(
                () => FatherUnbindHelpers.RunAdaptiveSampling(keyBeats, step, tolerance, ch));

            NrcToolLog.OnDebug(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}