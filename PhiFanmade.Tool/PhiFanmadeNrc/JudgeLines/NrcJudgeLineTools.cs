using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;
using PhiFanmade.Tool.Common;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;

/// <summary>
/// NRC 格式判定线操作工具。提供父子关系解绑功能。
/// </summary>
public static class NrcJudgeLineTools
{
    /// <summary>根据父线绝对坐标和旋转角度，计算子线的绝对坐标。</summary>
    public static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    /// <summary>根据指定渲染坐标系计算子线绝对坐标。</summary>
    public static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, CoordinateProfile renderProfile)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);
    }

    #region 经典（固定采样）

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，同步）。</summary>
    public static Nrc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => FatherUnbindProcessor.FatherUnbind(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines), compress);

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，同步，指定渲染坐标系）。</summary>
    public static Nrc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbind(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance, compress);
    }

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，异步）。</summary>
    public static async Task<Nrc.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => await FatherUnbindAsyncProcessor.FatherUnbindAsync(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines), compress);

    /// <summary>将判定线与父判定线解绑并保持行为一致（等间隔采样，异步，指定渲染坐标系）。</summary>
    public static async Task<Nrc.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return await FatherUnbindAsync(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance, compress);
    }

    #endregion

    #region Plus（自适应采样）

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，同步）。</summary>
    public static Nrc.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => FatherUnbindProcessor.FatherUnbindPlus(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines));

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，同步，指定渲染坐标系）。</summary>
    public static Nrc.JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance);
    }

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，异步）。</summary>
    public static async Task<Nrc.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => await FatherUnbindAsyncProcessor.FatherUnbindPlusAsync(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines));

    /// <summary>将判定线与父判定线解绑并保持行为一致（自适应采样，异步，指定渲染坐标系）。</summary>
    public static async Task<Nrc.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex, List<Nrc.JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return await FatherUnbindPlusAsync(targetJudgeLineIndex, allJudgeLines, precision, tolerance);
    }

    #endregion
}

