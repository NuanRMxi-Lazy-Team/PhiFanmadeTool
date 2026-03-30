using PhiFanmade.Core.Common;
using PhiFanmade.Tool.Common;

namespace PhiFanmade.Tool.RePhiEdit.Converters;

/// <summary>
/// RPE 格式 → NRC 格式转换器。
/// </summary>
public static class RpeToNrc
{
    public static Nrc.Chart Convert(Rpe.Chart rpe) => new()
    {
        BpmList = rpe.BpmList.ConvertAll(ConvertBpmItem),
        Meta = ConvertMeta(rpe.Meta),
        JudgeLineList = rpe.JudgeLineList.ConvertAll(ConvertJudgeLine)
    };

    private static int MapEasingNumber(int rpe) => rpe switch
    {
        1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
        8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
        14 => 15, 15 => 14, 16 => 18, 17 => 17, 18 => 21, 19 => 20,
        20 => 24, 21 => 23, 22 => 22, 23 => 25, 24 => 27, 25 => 26,
        26 => 30, 27 => 29, 28 => 31, 29 => 28, _ => 1
    };

    private static Nrc.Easing ConvertEasing(Rpe.Easing src) => new(MapEasingNumber((int)src));
    private static double TransformX(float x) => CoordinateGeometry.ToNrcX(x);
    private static double TransformY(float y) => CoordinateGeometry.ToNrcY(y);
    private static double TransformAngle(float angle) => CoordinateGeometry.ToNrcAngle(angle);

    private static Nrc.BpmItem ConvertBpmItem(Rpe.BpmItem src) => new()
    {
        Bpm = src.BeatPerMinute,
        StartBeat = new Beat((int[])src.StartBeat)
    };

    private static Nrc.Meta ConvertMeta(Rpe.Meta src) => new()
    {
        Background = src.Background,
        Author = src.Charter,
        Composer = src.Composer,
        Artist = src.Illustration,
        Level = src.Level,
        Name = src.Name,
        Offset = src.Offset,
        Song = src.Song
    };

    private static Nrc.JudgeLine ConvertJudgeLine(Rpe.JudgeLine src) => new()
    {
        Name = src.Name,
        Texture = src.Texture,
        Anchor = (float[])src.Anchor.Clone(),
        Father = src.Father,
        IsCover = src.IsCover,
        ZOrder = src.ZOrder,
        AttachUi = src.AttachUi.HasValue ? (Nrc.AttachUi?)(int)src.AttachUi.Value : null,
        IsGif = src.IsGif,
        BpmFactor = src.BpmFactor,
        RotateWithFather = src.RotateWithFather,
        Notes = src.Notes.ConvertAll(ConvertNote),
        EventLayers = src.EventLayers.ConvertAll(ConvertEventLayer),
        Extended = ConvertExtendLayer(src.Extended),
        PositionControls = src.PositionControls.ConvertAll(ConvertXControl),
        AlphaControls = src.AlphaControls.ConvertAll(ConvertAlphaControl),
        SizeControls = src.SizeControls.ConvertAll(ConvertSizeControl),
        SkewControls = src.SkewControls.ConvertAll(ConvertSkewControl),
        YControls = src.YControls.ConvertAll(ConvertYControl)
    };

    private static Nrc.Note ConvertNote(Rpe.Note src) => new()
    {
        Above = src.Above,
        Alpha = src.Alpha,
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        IsFake = src.IsFake,
        PositionX = TransformX(src.PositionX),
        Size = src.Size,
        JudgeArea = src.JudgeArea,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Nrc.NoteType)(int)src.Type,
        VisibleTime = src.VisibleTime,
        YOffset = TransformY(src.YOffset),
        Tint = src.Color.ToArray(),
        HitFxColor = src.HitFxColor?.ToArray(),
        HitSound = src.HitSound
    };

    private static Nrc.EventLayer ConvertEventLayer(Rpe.EventLayer src)
    {
        var nrc = new Nrc.EventLayer();
        if (src.MoveXEvents != null) nrc.MoveXEvents = src.MoveXEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformX));
        if (src.MoveYEvents != null) nrc.MoveYEvents = src.MoveYEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformY));
        if (src.RotateEvents != null) nrc.RotateEvents = src.RotateEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformAngle));
        if (src.AlphaEvents != null) nrc.AlphaEvents = src.AlphaEvents.ConvertAll(ConvertIntEvent);
        if (src.SpeedEvents != null) nrc.SpeedEvents = src.SpeedEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static Nrc.ExtendLayer? ConvertExtendLayer(Rpe.ExtendLayer? src)
    {
        if (src == null) return null;
        var nrc = new Nrc.ExtendLayer();
        if (src.ColorEvents != null) nrc.ColorEvents = src.ColorEvents.ConvertAll(ConvertByteArrayEvent);
        if (src.ScaleXEvents != null) nrc.ScaleXEvents = src.ScaleXEvents.ConvertAll(ConvertFloatEvent);
        if (src.ScaleYEvents != null) nrc.ScaleYEvents = src.ScaleYEvents.ConvertAll(ConvertFloatEvent);
        if (src.TextEvents != null) nrc.TextEvents = src.TextEvents.ConvertAll(ConvertStringEvent);
        if (src.PaintEvents != null) nrc.PaintEvents = src.PaintEvents.ConvertAll(ConvertFloatEvent);
        if (src.GifEvents != null) nrc.GifEvents = src.GifEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static Nrc.Event<T> ConvertEvent<T>(Rpe.Event<T> src, Func<T, T>? valueCopier = null, Func<T, T>? valueTransformer = null)
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new Nrc.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = src.BezierPoints.ToArray(),
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = ConvertEasing(src.Easing),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    private static Nrc.Event<double> ConvertFloatToDoubleEvent(Rpe.Event<float> src, Func<float, double> valueTransformer) => new()
    {
        IsBezier = src.IsBezier,
        BezierPoints = src.BezierPoints.ToArray(),
        EasingLeft = src.EasingLeft,
        EasingRight = src.EasingRight,
        Easing = ConvertEasing(src.Easing),
        StartValue = valueTransformer(src.StartValue),
        EndValue = valueTransformer(src.EndValue),
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        Font = src.Font
    };

    private static Nrc.Event<float> ConvertFloatEvent(Rpe.Event<float> src) => ConvertEvent(src);
    private static Nrc.Event<int> ConvertIntEvent(Rpe.Event<int> src) => ConvertEvent(src);
    private static Nrc.Event<string> ConvertStringEvent(Rpe.Event<string> src) => ConvertEvent(src);
    private static Nrc.Event<byte[]> ConvertByteArrayEvent(Rpe.Event<byte[]> src) => ConvertEvent(src, v => v?.ToArray());
    private static Nrc.XControl ConvertXControl(Rpe.XControl src) => new() { Easing = ConvertEasing(src.Easing), X = src.X, Pos = src.Pos };
    private static Nrc.AlphaControl ConvertAlphaControl(Rpe.AlphaControl src) => new() { Easing = ConvertEasing(src.Easing), X = src.X, Alpha = src.Alpha };
    private static Nrc.SizeControl ConvertSizeControl(Rpe.SizeControl src) => new() { Easing = ConvertEasing(src.Easing), X = src.X, Size = src.Size };
    private static Nrc.SkewControl ConvertSkewControl(Rpe.SkewControl src) => new() { Easing = ConvertEasing(src.Easing), X = src.X, Skew = src.Skew };
    private static Nrc.YControl ConvertYControl(Rpe.YControl src) => new() { Easing = ConvertEasing(src.Easing), X = src.X, Y = src.Y };
}
