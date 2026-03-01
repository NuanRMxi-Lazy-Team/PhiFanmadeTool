namespace PhiFanmade.Tool.Utils;

public static class PhiEditUtility
{
    public static class CoordinateTransform
    {
        public static float ToRePhiEditX(float x)
        {
            var rpeMin = Rpe.Chart.CoordinateSystem.MinX;
            var rpeMax = Rpe.Chart.CoordinateSystem.MaxX;
            var peMin = Pe.Chart.CoordinateSystem.MinX;
            var peMax = Pe.Chart.CoordinateSystem.MaxX;
            return rpeMin + (x - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }

        public static float ToRePhiEditY(float y)
        {
            var rpeMin = Rpe.Chart.CoordinateSystem.MinY;
            var rpeMax = Rpe.Chart.CoordinateSystem.MaxY;
            var peMin = Rpe.Chart.CoordinateSystem.MinY;
            var peMax = Rpe.Chart.CoordinateSystem.MaxY;
            return rpeMin + (y - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }
    }
}