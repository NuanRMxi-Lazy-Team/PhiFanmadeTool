namespace PhiFanmade.Tool.Converter;

public static class RePhiEditToPhiEdit
{
    public static Pe.Chart ConvertChart(Rpe.Chart rpeChart)
    {
        var peChart = new Pe.Chart();
        // BPMs
        foreach (var rpeBpm in rpeChart.BpmList)
            peChart.BpmList.Add(new Pe.Bpm
                { BeatPerMinute = rpeBpm.BeatPerMinute, StartBeat = rpeBpm.StartTime });
        // JudgeLines
        return null;
    }
}