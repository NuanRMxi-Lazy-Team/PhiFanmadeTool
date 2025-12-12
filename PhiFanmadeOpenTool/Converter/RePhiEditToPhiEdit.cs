using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;

namespace PhiFanmade.OpenTool.Converter;

public static class RePhiEditToPhiEdit
{
    public static class CoordinateTransform
    {
        public static float ToPhiEditX(float rePhiEditX)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxX;
            return (rePhiEditX - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }

        public static float ToPhiEditY(float rePhiEditY)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxY;
            return (rePhiEditY - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }
    }

    public static PhiEdit.Chart ConvertChart(RePhiEdit.Chart rpeChart)
    {
        var peChart = new PhiEdit.Chart();
        // BPMs
        foreach (var rpeBpm in rpeChart.BpmList)
            peChart.BpmList.Add(new PhiEdit.Bpm
                { BeatPerMinute = rpeBpm.BeatPerMinute, StartBeat = rpeBpm.StartTime });
        // JudgeLines
        return null;
    }
}