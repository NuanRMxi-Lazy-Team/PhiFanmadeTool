using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class BpmItem
{
    public static Kpc.BpmItem ConvertBpmItem(Pe.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = new Beat(src.StartBeat)
    };

    public static Pe.BpmItem ConvertBpmItem(Kpc.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = (float)(double)src.StartBeat
    };
}
