using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;

namespace PhiFanmade.OpenTool.Converter;

public static class RePhiEditToPhiEdit
{
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