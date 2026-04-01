using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑同步处理器。
/// 所有采样算法均委托给 <see cref="FatherUnbindHelpers"/> 中的共享实现，
/// 本类只负责缓存检查、父线递归解绑、通道合并及日志记录。
/// </summary>
internal static class FatherUnbindProcessor
{
    private readonly record struct PrepareResult(
        Nrc.JudgeLine JudgeLine,
        Nrc.JudgeLine? FatherLine,
        bool ShouldReturn);

    // 抽取同步版的前置流程，保证 FatherUnbind / FatherUnbindPlus 在缓存、父链递归和层清理上行为一致。
    private static PrepareResult PrepareUnbindContext(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        string logTag,
        string startAction,
        Func<int, List<Nrc.JudgeLine>, Nrc.JudgeLine> recursiveUnbind)
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

        // 若父线仍有父线，递归解绑父链，确保父线已为绝对坐标
        var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
        if (fatherLineCopy.Father >= 0)
        {
            NrcToolLog.OnDebug($"{logTag}[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
            fatherLineCopy = recursiveUnbind(judgeLineCopy.Father, allJudgeLinesCopy);
        }

        FatherUnbindHelpers.CleanupRedundantLayers(judgeLineCopy, fatherLineCopy);

        return new PrepareResult(judgeLineCopy, fatherLineCopy, false);
    }

    /// <summary>
    /// 等间隔采样解绑（同步版）：将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归解绑父链 → 合并各通道 → 按步长切割 → 并行等间隔采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">误差容差百分比，用于事件压缩。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
    /// <param name="compress">是否在写回前压缩冗余事件。</param>
    internal static Nrc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        bool compress = true)
    {
        Nrc.JudgeLine judgeLineCopy;
        try
        {
            // 统一前置流程，减少同步/自适应两个入口重复代码。
            var prepare = PrepareUnbindContext(
                targetJudgeLineIndex,
                allJudgeLines,
                cache,
                logTag: "FatherUnbind",
                startAction: "开始解绑",
                recursiveUnbind: (idx, lines) => FatherUnbind(idx, lines, precision, tolerance, cache, compress));

            judgeLineCopy = prepare.JudgeLine;
            if (prepare.ShouldReturn)
                return judgeLineCopy;

            var fatherLine = prepare.FatherLine;
            if (fatherLine is null)
                return judgeLineCopy;

            // 等间隔版使用 EventListMerge 进行层间事件合并
            List<Nrc.Event<double>> Merge(List<Nrc.Event<double>> a, List<Nrc.Event<double>> b)
                => EventMerger.EventListMerge(a, b, precision, tolerance, compress);

            var mergedChannels = FatherUnbindHelpers.MergeChannels(judgeLineCopy.EventLayers, fatherLine.EventLayers, Merge);

            // 将各通道按精度步长切割，保证等间隔采样时每个步长内只有一段事件
            var cutLength = new Beat(1d / precision);
            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Tx);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Ty);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fx);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fy);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fr);

            var cutTasks = new[]
            {
                Task.Run(() => EventCutter.CutEventsInRange(mergedChannels.Tx, txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergedChannels.Ty, tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergedChannels.Fx, fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergedChannels.Fy, fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergedChannels.Fr, frMin, frMax, cutLength))
            };
            Task.WaitAll(cutTasks);

            // cutTasks 顺序：[0]=tx, [1]=ty, [2]=fx, [3]=fy, [4]=fr
            var cutChannels = new FatherUnbindHelpers.EventChannels(
                Fx: cutTasks[2].Result, Fy: cutTasks[3].Result, Fr: cutTasks[4].Result,
                Tx: cutTasks[0].Result, Ty: cutTasks[1].Result);

            var overallMin = new Beat(Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin));
            var overallMax = new Beat(Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax));
            var step = new Beat(1d / precision);
            var beats = FatherUnbindHelpers.BuildBeatList(overallMin, overallMax, step);

            NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");
            var (sortedX, sortedY) = FatherUnbindHelpers.EqualSpacingSampling(beats, overallMax, step, cutChannels);

            NrcToolLog.OnDebug($"FatherUnbind[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, cutChannels.Fr, tolerance, Merge, compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbind[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
            NrcToolLog.OnError($"FatherUnbind[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 自适应采样解绑（同步版）：以事件边界为强制切割点，仅在误差超过容差时插入新采样段，
    /// 相较等间隔版可减少冗余段数。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归解绑父链 → 合并各通道 → 收集关键帧 → 并行自适应采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">自适应采样的最大步数上限（同时作为事件合并精度）。</param>
    /// <param name="tolerance">误差容差百分比，决定何时插入额外切割点及压缩阈值。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
    internal static Nrc.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache)
    {
        Nrc.JudgeLine judgeLineCopy;
        try
        {
            // 与 FatherUnbind 复用同一前置流程，降低维护分叉风险。
            var (judgeLine, fatherLine, shouldReturn) = PrepareUnbindContext(
                targetJudgeLineIndex,
                allJudgeLines,
                cache,
                logTag: "FatherUnbindPlus",
                startAction: "开始解绑（自适应采样）",
                recursiveUnbind: (idx, lines) => FatherUnbindPlus(idx, lines, precision, tolerance, cache));

            judgeLineCopy = judgeLine;
            if (shouldReturn || fatherLine is null)
                return judgeLineCopy;

            var ch = FatherUnbindHelpers.MergeChannels(judgeLineCopy.EventLayers, fatherLine.EventLayers, Merge);

            // 确定总体拍范围；若所有通道均为空则无需采样，直接解绑
            var rangeResult = FatherUnbindHelpers.TryGetOverallRange(ch);
            if (rangeResult is null)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);
            var keyBeats = FatherUnbindHelpers.CollectKeyBeats(overallMin, overallMax, ch);

            NrcToolLog.OnDebug(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");
            var (resultX, resultY) = FatherUnbindHelpers.RunAdaptiveSampling(keyBeats, step, tolerance, ch);

            NrcToolLog.OnDebug(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(
                judgeLineCopy, resultX, resultY, ch.Fr, tolerance, Merge, compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;

            // 自适应版使用 EventMergePlus 进行层间事件合并
            List<Nrc.Event<double>> Merge(List<Nrc.Event<double>> a, List<Nrc.Event<double>> b)
                => EventMerger.EventMergePlus(a, b, precision, tolerance);
        }
        catch (Exception ex)
        {
            judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
            NrcToolLog.OnError($"FatherUnbindPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}