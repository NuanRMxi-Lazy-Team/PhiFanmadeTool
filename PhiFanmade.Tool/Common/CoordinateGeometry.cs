namespace PhiFanmade.Tool.Common;

/// <summary>
/// 坐标几何换算工具。
/// NRC 作为统一归一化坐标空间；几何计算在可配置的渲染空间进行，
/// 以避免非等比坐标系下的旋转与误差评估失真。
/// </summary>
internal static class CoordinateGeometry
{
    private static readonly CoordinateProfile NrcProfile = CoordinateProfile.NrcProfile;
    private static readonly CoordinateProfile RenderProfileDefault = CoordinateProfile.DefaultRenderProfile;

    /// <summary>
    /// 计算坐标轴跨度，并校验跨度不为零。
    /// </summary>
    /// <param name="min">坐标轴最小值。</param>
    /// <param name="max">坐标轴最大值。</param>
    /// <param name="axisName">轴名称，仅用于异常信息。</param>
    /// <returns>坐标轴跨度。</returns>
    /// <exception cref="InvalidOperationException">当坐标轴跨度接近零时抛出。</exception>
    private static double GetSpan(double min, double max, string axisName)
    {
        var span = max - min;
        return Math.Abs(span) < 1e-12
            ? throw new InvalidOperationException($"Coordinate span of axis '{axisName}' is zero.")
            : span;
    }

    /// <summary>
    /// 将绝对坐标值从源区间线性映射到目标区间。
    /// </summary>
    /// <param name="value">待映射的源坐标值。</param>
    /// <param name="sourceMin">源区间最小值。</param>
    /// <param name="sourceMax">源区间最大值。</param>
    /// <param name="targetMin">目标区间最小值。</param>
    /// <param name="targetMax">目标区间最大值。</param>
    /// <param name="axisName">轴名称，仅用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标值。</returns>
    private static double MapValue(double value, double sourceMin, double sourceMax, double targetMin, double targetMax,
        string axisName)
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return targetMin + (value - sourceMin) / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将坐标增量从源区间按比例映射到目标区间。
    /// </summary>
    /// <param name="delta">待映射的源坐标增量。</param>
    /// <param name="sourceMin">源区间最小值。</param>
    /// <param name="sourceMax">源区间最大值。</param>
    /// <param name="targetMin">目标区间最小值。</param>
    /// <param name="targetMax">目标区间最大值。</param>
    /// <param name="axisName">轴名称，仅用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标增量。</returns>
    private static double MapDelta(double delta, double sourceMin, double sourceMax, double targetMin, double targetMax,
        string axisName)
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return delta / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将 NRC X 坐标映射到目标坐标系。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    private static double ToTargetXCore(double x, CoordinateProfile target)
        => MapValue(x, NrcProfile.MinX, NrcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 NRC Y 坐标映射到目标坐标系。
    /// </summary>
    /// <param name="y">NRC Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    private static double ToTargetYCore(double y, CoordinateProfile target)
        => MapValue(y, NrcProfile.MinY, NrcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将源坐标系 X 坐标映射到 NRC。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC 坐标系下的 X 坐标。</returns>
    private static double ToNrcXCore(double x, CoordinateProfile source)
        => MapValue(x, source.MinX, source.MaxX, NrcProfile.MinX, NrcProfile.MaxX, "X");

    /// <summary>
    /// 将源坐标系 Y 坐标映射到 NRC。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC 坐标系下的 Y 坐标。</returns>
    private static double ToNrcYCore(double y, CoordinateProfile source)
        => MapValue(y, source.MinY, source.MaxY, NrcProfile.MinY, NrcProfile.MaxY, "Y");

    /// <summary>
    /// 将 NRC X 增量映射到目标坐标系增量。
    /// </summary>
    /// <param name="x">NRC X 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 增量。</returns>
    private static double ToTargetDeltaXCore(double x, CoordinateProfile target)
        => MapDelta(x, NrcProfile.MinX, NrcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 NRC Y 增量映射到目标坐标系增量。
    /// </summary>
    /// <param name="y">NRC Y 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 增量。</returns>
    private static double ToTargetDeltaYCore(double y, CoordinateProfile target)
        => MapDelta(y, NrcProfile.MinY, NrcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将 NRC 角度转换到目标坐标系角度方向。
    /// </summary>
    /// <param name="nrcAngleDegrees">NRC 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的角度（度）。</returns>
    private static double ToTargetAngleCore(double nrcAngleDegrees, CoordinateProfile target)
        => target.ClockwiseRotation == NrcProfile.ClockwiseRotation ? nrcAngleDegrees : -nrcAngleDegrees;

    /// <summary>
    /// 将源坐标系角度转换到 NRC 角度方向。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC 角度（度）。</returns>
    private static double ToNrcAngleCore(double sourceAngleDegrees, CoordinateProfile source)
        => source.ClockwiseRotation == NrcProfile.ClockwiseRotation ? sourceAngleDegrees : -sourceAngleDegrees;

    /// <summary>
    /// 将 NRC X 坐标转换为指定坐标系 X 坐标。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    internal static double ToTargetX(double x, in CoordinateProfile target) => ToTargetXCore(x, target);
    /// <summary>
    /// 将 NRC Y 坐标转换为指定坐标系 Y 坐标。
    /// </summary>
    /// <param name="y">NRC Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    internal static double ToTargetY(double y, in CoordinateProfile target) => ToTargetYCore(y, target);
    /// <summary>
    /// 将 NRC X 坐标转换为指定坐标系 X 坐标（单精度）。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的单精度 X 坐标。</returns>
    internal static float ToTargetXf(double x, in CoordinateProfile target) => (float)ToTargetXCore(x, target);
    /// <summary>
    /// 将 NRC Y 坐标转换为指定坐标系 Y 坐标（单精度）。
    /// </summary>
    /// <param name="y">NRC Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的单精度 Y 坐标。</returns>
    internal static float ToTargetYf(double y, in CoordinateProfile target) => (float)ToTargetYCore(y, target);
    /// <summary>
    /// 将指定坐标系 X 坐标转换为 NRC X 坐标。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC X 坐标。</returns>
    internal static double ToNrcX(double x, in CoordinateProfile source) => ToNrcXCore(x, source);
    /// <summary>
    /// 将指定坐标系 Y 坐标转换为 NRC Y 坐标。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC Y 坐标。</returns>
    internal static double ToNrcY(double y, in CoordinateProfile source) => ToNrcYCore(y, source);

    /// <summary>
    /// 将 NRC 角度转换为指定坐标系角度。
    /// </summary>
    /// <param name="nrcAngleDegrees">NRC 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系角度（度）。</returns>
    internal static double ToTargetAngle(double nrcAngleDegrees, in CoordinateProfile target)
        => ToTargetAngleCore(nrcAngleDegrees, target);

    /// <summary>
    /// 将指定坐标系角度转换为 NRC 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>NRC 角度（度）。</returns>
    internal static double ToNrcAngle(double sourceAngleDegrees, in CoordinateProfile source)
        => ToNrcAngleCore(sourceAngleDegrees, source);

    /// <summary>
    /// 将 NRC X 坐标转换为默认渲染坐标系 X 坐标。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <returns>默认渲染坐标系下的 X 坐标。</returns>
    internal static double ToRenderX(double x) => ToTargetXCore(x, RenderProfileDefault);
    /// <summary>
    /// 将 NRC Y 坐标转换为默认渲染坐标系 Y 坐标。
    /// </summary>
    /// <param name="y">NRC Y 坐标。</param>
    /// <returns>默认渲染坐标系下的 Y 坐标。</returns>
    internal static double ToRenderY(double y) => ToTargetYCore(y, RenderProfileDefault);
    /// <summary>
    /// 将 NRC X 坐标转换为默认渲染坐标系 X 坐标（单精度）。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <returns>默认渲染坐标系下的单精度 X 坐标。</returns>
    internal static float ToRenderXf(double x) => (float)ToRenderX(x);
    /// <summary>
    /// 将 NRC Y 坐标转换为默认渲染坐标系 Y 坐标（单精度）。
    /// </summary>
    /// <param name="y">NRC Y 坐标。</param>
    /// <returns>默认渲染坐标系下的单精度 Y 坐标。</returns>
    internal static float ToRenderYf(double y) => (float)ToRenderY(y);

    /// <summary>
    /// 将默认渲染坐标系 X 坐标转换为 NRC X 坐标。
    /// </summary>
    /// <param name="x">默认渲染坐标系 X 坐标。</param>
    /// <returns>NRC X 坐标。</returns>
    internal static double ToNrcX(double x) => ToNrcXCore(x, RenderProfileDefault);
    /// <summary>
    /// 将默认渲染坐标系 Y 坐标转换为 NRC Y 坐标。
    /// </summary>
    /// <param name="y">默认渲染坐标系 Y 坐标。</param>
    /// <returns>NRC Y 坐标。</returns>
    internal static double ToNrcY(double y) => ToNrcYCore(y, RenderProfileDefault);

    /// <summary>
    /// 将 NRC 角度转换为默认渲染坐标系角度。
    /// </summary>
    /// <param name="nrcAngleDegrees">NRC 角度（度）。</param>
    /// <returns>默认渲染坐标系角度（度）。</returns>
    internal static double ToRenderAngle(double nrcAngleDegrees) =>
        ToTargetAngleCore(nrcAngleDegrees, RenderProfileDefault);

    /// <summary>
    /// 将默认渲染坐标系角度转换为 NRC 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">默认渲染坐标系角度（度）。</param>
    /// <returns>NRC 角度（度）。</returns>
    internal static double ToNrcAngle(double sourceAngleDegrees) =>
        ToNrcAngleCore(sourceAngleDegrees, RenderProfileDefault);

    /// <summary>
    /// 将 NRC 点坐标转换为默认渲染坐标系点坐标。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <param name="y">NRC Y 坐标。</param>
    /// <returns>默认渲染坐标系点坐标。</returns>
    internal static (double X, double Y) ToRenderPoint(double x, double y)
        => (ToRenderX(x), ToRenderY(y));

    /// <summary>
    /// 将 NRC 点坐标转换为指定坐标系点坐标。
    /// </summary>
    /// <param name="x">NRC X 坐标。</param>
    /// <param name="y">NRC Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系点坐标。</returns>
    internal static (double X, double Y) ToTargetPoint(double x, double y, in CoordinateProfile target)
        => (ToTargetXCore(x, target), ToTargetYCore(y, target));

    /// <summary>
    /// 在默认渲染坐标系下旋转 NRC 增量向量，并将结果映射回 NRC。
    /// </summary>
    /// <param name="x">NRC X 增量。</param>
    /// <param name="y">NRC Y 增量。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <returns>旋转后的 NRC 增量向量。</returns>
    internal static (double X, double Y) RotateNrcOffset(double x, double y, double angleDegrees)
        => RotateNrcOffset(x, y, angleDegrees, RenderProfileDefault);

    /// <summary>
    /// 在指定渲染坐标系下旋转 NRC 增量向量，并将结果映射回 NRC。
    /// </summary>
    /// <param name="x">NRC X 增量。</param>
    /// <param name="y">NRC Y 增量。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <param name="renderProfile">用于旋转计算的渲染坐标配置。</param>
    /// <returns>旋转后的 NRC 增量向量。</returns>
    internal static (double X, double Y) RotateNrcOffset(
        double x, double y, double angleDegrees, in CoordinateProfile renderProfile)
    {
        var renderX = ToTargetDeltaXCore(x, renderProfile);
        var renderY = ToTargetDeltaYCore(y, renderProfile);
        var rad = angleDegrees * (Math.PI / 180d);
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        var rotatedRenderX = renderX * cos - renderY * sin;
        var rotatedRenderY = renderX * sin + renderY * cos;
        return (ToNrcXCore(rotatedRenderX, renderProfile), ToNrcYCore(rotatedRenderY, renderProfile));
    }

    /// <summary>
    /// 使用默认渲染坐标配置，计算子线在 NRC 下的绝对坐标。
    /// </summary>
    /// <param name="fatherLineX">父线 NRC X 坐标。</param>
    /// <param name="fatherLineY">父线 NRC Y 坐标。</param>
    /// <param name="angleDegrees">子线相对旋转角度（度）。</param>
    /// <param name="lineX">子线相对 NRC X 偏移。</param>
    /// <param name="lineY">子线相对 NRC Y 偏移。</param>
    /// <returns>子线 NRC 绝对坐标。</returns>
    internal static (double X, double Y) GetNrcAbsolutePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => GetNrcAbsolutePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY, RenderProfileDefault);

    /// <summary>
    /// 使用指定渲染坐标配置，计算子线在 NRC 下的绝对坐标。
    /// </summary>
    /// <param name="fatherLineX">父线 NRC X 坐标。</param>
    /// <param name="fatherLineY">父线 NRC Y 坐标。</param>
    /// <param name="angleDegrees">子线相对旋转角度（度）。</param>
    /// <param name="lineX">子线相对 NRC X 偏移。</param>
    /// <param name="lineY">子线相对 NRC Y 偏移。</param>
    /// <param name="renderProfile">用于旋转计算的渲染坐标配置。</param>
    /// <returns>子线 NRC 绝对坐标。</returns>
    internal static (double X, double Y) GetNrcAbsolutePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, in CoordinateProfile renderProfile)
    {
        var (rotX, rotY) = RotateNrcOffset(lineX, lineY, angleDegrees, renderProfile);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 基于默认渲染坐标配置，计算 NRC 点在屏幕空间中的模长。
    /// </summary>
    /// <param name="point">NRC 点坐标。</param>
    /// <returns>屏幕空间模长。</returns>
    internal static double GetNrcScreenMagnitude((double X, double Y) point)
        => GetNrcScreenMagnitude(point, RenderProfileDefault);

    /// <summary>
    /// 基于指定渲染坐标配置，计算 NRC 点在屏幕空间中的模长。
    /// </summary>
    /// <param name="point">NRC 点坐标。</param>
    /// <param name="renderProfile">用于屏幕空间计算的渲染坐标配置。</param>
    /// <returns>屏幕空间模长。</returns>
    internal static double GetNrcScreenMagnitude((double X, double Y) point, in CoordinateProfile renderProfile)
    {
        var (renderX, renderY) = ToTargetPoint(point.X, point.Y, renderProfile);
        return Math.Sqrt(renderX * renderX + renderY * renderY);
    }

    /// <summary>
    /// 基于默认渲染坐标配置，计算两个 NRC 点在屏幕空间中的距离。
    /// </summary>
    /// <param name="left">第一个 NRC 点。</param>
    /// <param name="right">第二个 NRC 点。</param>
    /// <returns>屏幕空间距离。</returns>
    internal static double GetNrcScreenDistance((double X, double Y) left, (double X, double Y) right)
        => GetNrcScreenDistance(left, right, RenderProfileDefault);

    /// <summary>
    /// 基于指定渲染坐标配置，计算两个 NRC 点在屏幕空间中的距离。
    /// </summary>
    /// <param name="left">第一个 NRC 点。</param>
    /// <param name="right">第二个 NRC 点。</param>
    /// <param name="renderProfile">用于屏幕空间计算的渲染坐标配置。</param>
    /// <returns>屏幕空间距离。</returns>
    internal static double GetNrcScreenDistance(
        (double X, double Y) left,
        (double X, double Y) right,
        in CoordinateProfile renderProfile)
    {
        var deltaRenderX = ToTargetDeltaXCore(left.X - right.X, renderProfile);
        var deltaRenderY = ToTargetDeltaYCore(left.Y - right.Y, renderProfile);
        return Math.Sqrt(deltaRenderX * deltaRenderX + deltaRenderY * deltaRenderY);
    }
}