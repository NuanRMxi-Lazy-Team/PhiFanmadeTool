namespace PhiFanmade.OpenTool.Utils;

public static class PhiEdit
{
    public static class CoordinateTransform
    {
        public static float ToRePhiEditX(float x)
        {
            var rpeMin = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MaxX;
            return rpeMin + (x - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }
        
        public static float ToRePhiEditY(float y)
        {
            var rpeMin = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = Core.RePhiEdit.RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = Core.PhiEdit.PhiEdit.Chart.CoordinateSystem.MaxY;
            return rpeMin + (y - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }
    }
}