namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class JudgeLineConverter
{
    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    public static List<Kpc.JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines)
    {
        if (judgeLines == null || judgeLines.Count == 0) return [];

        var result = new List<Kpc.JudgeLine>(judgeLines.Count);
        for (var i = 0; i < judgeLines.Count; i++)
            result.Add(ConvertJudgeLine(judgeLines[i], i));
        return result;
    }

    /// <summary>
    /// 转换单条判定线，并合成为单事件层的 KPC 判定线。
    /// </summary>
    public static Kpc.JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index)
    {
        var horizonBeat = FrameEventInterpolator.GetJudgeLineHorizonBeat(src);
        var eventLayer = EventLayerConverter.ConvertEventLayer(src, horizonBeat);
        eventLayer.Anticipation();

        return new Kpc.JudgeLine
        {
            Name = $"PeJudgeLine_{index}",
            Notes = src.NoteList?.ConvertAll(Note.ConvertNote) ?? [],
            EventLayers = [eventLayer]
        };
    }
}
