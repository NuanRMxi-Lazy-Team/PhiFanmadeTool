using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;

namespace PhiFanmade.OpenTool.Converter;

public static class PhiEditToRePhiEdit
{
    public static class CoordinateTransform
    {
        public static float ToRePhiEditX(float x)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxX;
            return rpeMin + (x - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }
        
        public static float ToRePhiEditY(float y)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxY;
            return rpeMin + (y - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
        }
    }
}