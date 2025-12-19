using static PhiFanmade.Core.RePhiEdit.RePhiEdit;

namespace PhiFanmade.OpenTool.Utils.RePhiEditUtility;

/// <summary>
/// 判定线父子关系处理器
/// </summary>
internal static class FatherUnbindProcessor
{
    /// <summary>
    /// 在有父线的情况下，获得一条判定线的绝对位置
    /// </summary>
    /// <param name="fatherLineX">父线X轴坐标</param>
    /// <param name="fatherLineY">父线Y轴坐标</param>
    /// <param name="angleDegrees">父线旋转角度</param>
    /// <param name="lineX">当前线相对于父线的X轴坐标</param>
    /// <param name="lineY">当前线相对于父线的Y轴坐标</param>
    /// <returns>当前线绝对坐标</returns>
    internal static (double, double) GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        // 将角度转换为弧度
        double angleRadians = (angleDegrees % 360) * Math.PI / 180f;

        // 计算旋转后的坐标
        double rotatedX = lineX * Math.Cos(angleRadians) + lineY * Math.Sin(angleRadians);
        double rotatedY = -lineX * Math.Sin(angleRadians) + lineY * Math.Cos(angleRadians);

        // 计算绝对坐标
        double absoluteX = fatherLineX + rotatedX;
        double absoluteY = fatherLineY + rotatedY;

        return (absoluteX, absoluteY);
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">切割精度，默认64分之一拍</param>
    /// <param name="tolerance">拟合容差，越大拟合精细度越低</param>
    /// <returns></returns>
    public static JudgeLine FatherUnbind(int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d) =>
        FatherUnbindAsyncProcessor.FatherUnbindCore(targetJudgeLineIndex, allJudgeLines, precision, tolerance);
}