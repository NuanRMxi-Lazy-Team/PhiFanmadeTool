using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class Note
{
    public static Kpc.Note ConvertNote(Pe.Note src) => new()
    {
        Above = src.Above,
        StartBeat = new Beat(src.StartBeat),
        EndBeat = new Beat(src.EndBeat),
        IsFake = src.IsFake,
        PositionX = Transform.TransformToKpcX(src.PositionX) + Kpc.Chart.CoordinateSystem.MaxX,
        WidthRatio = src.WidthRatio,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Kpc.NoteType)(int)src.Type
    };

    public static Pe.Note ConvertNote(Kpc.Note src) => new()
    {
        Above = src.Above,
        StartBeat = (float)(double)src.StartBeat,
        EndBeat = (float)(double)src.EndBeat,
        IsFake = src.IsFake,
        PositionX = Transform.TransformToPeX(src.PositionX - Kpc.Chart.CoordinateSystem.MaxX),
        WidthRatio = src.WidthRatio,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Pe.NoteType)(int)src.Type
    };
}
