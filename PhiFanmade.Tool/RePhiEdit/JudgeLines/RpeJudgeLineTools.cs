using PhiFanmade.Tool.RePhiEdit.JudgeLines.Internal;

namespace PhiFanmade.Tool.RePhiEdit.JudgeLines;

/// <summary>
/// RPE 格式判定线操作工具。提供父子关系解绑功能。
/// </summary>
public static class RpeJudgeLineTools
{
    /// <summary>根据父线绝对坐标和旋转角度，计算子线的绝对坐标。</summary>
    public static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    #region 经典（固定采样）

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，同步）。</summary>
    public static Rpe.JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<Rpe.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => FatherUnbindProcessor.FatherUnbind(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines), compress);

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，异步）。</summary>
    public static async Task<Rpe.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex, List<Rpe.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d, bool useCompress = true)
        => await FatherUnbindAsyncProcessor.FatherUnbindAsync(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines), useCompress);

    #endregion

    #region Plus（自适应采样）

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，同步）。</summary>
    public static Rpe.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<Rpe.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => FatherUnbindProcessor.FatherUnbindPlus(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines));

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，异步）。</summary>
    public static async Task<Rpe.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex, List<Rpe.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => await FatherUnbindAsyncProcessor.FatherUnbindPlusAsync(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines));

    #endregion
}


