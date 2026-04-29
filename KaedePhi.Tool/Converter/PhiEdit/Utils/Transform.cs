using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class Transform
{
    private static readonly CoordinateProfile PeCoordinateProfile = new(
        Pe.Chart.CoordinateSystem.MinX,
        Pe.Chart.CoordinateSystem.MaxX,
        Pe.Chart.CoordinateSystem.MinY,
        Pe.Chart.CoordinateSystem.MaxY,
        Pe.Chart.CoordinateSystem.ClockwiseRotation);

    public static double TransformToKpcX(float x) => CoordinateGeometry.ToNrcX(x, PeCoordinateProfile);
    public static double TransformToKpcY(float y) => CoordinateGeometry.ToNrcY(y, PeCoordinateProfile);
    public static double TransformToKpcAngle(float angle) => CoordinateGeometry.ToNrcAngle(angle, PeCoordinateProfile);

    public static float TransformToPeX(double x) => CoordinateGeometry.ToTargetXf(x, PeCoordinateProfile);
    public static float TransformToPeY(double y) => CoordinateGeometry.ToTargetYf(y, PeCoordinateProfile);
    public static float TransformToPeAngle(double angle) => (float)CoordinateGeometry.ToTargetAngle(angle, PeCoordinateProfile);
}
