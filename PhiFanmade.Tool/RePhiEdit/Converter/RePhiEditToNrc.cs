using System.Collections.Generic;
using System.Linq;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Converter;

public static class RePhiEditToNrc
{
    public static Nrc.Chart Convert(Rpe.Chart rpe)
    {
        return new Nrc.Chart
        {
            BpmList = rpe.BpmList.ConvertAll(ConvertBpmItem),
            Meta = ConvertMeta(rpe.Meta),
            JudgeLineList = rpe.JudgeLineList.ConvertAll(ConvertJudgeLine)
        };
    }

    /// <summary>
    /// RPE 与 NRC 的缓动编号对应函数顺序不同，此方法负责将 RPE 编号转换为 NRC 编号。
    /// </summary>
    private static int MapEasingNumber(int rpe) => rpe switch
    {
        1 => 1, // Linear        -> Linear
        2 => 3, // EaseOutSine   -> EaseOutSine
        3 => 2, // EaseInSine    -> EaseInSine
        4 => 6, // EaseOutQuad   -> EaseOutQuad
        5 => 5, // EaseInQuad    -> EaseInQuad
        6 => 4, // EaseInOutSine -> EaseInOutSine
        7 => 7, // EaseInOutQuad -> EaseInOutQuad
        8 => 9, // EaseOutCubic  -> EaseOutCubic
        9 => 8, // EaseInCubic   -> EaseInCubic
        10 => 12, // EaseOutQuart  -> EaseOutQuart
        11 => 11, // EaseInQuart   -> EaseInQuart
        12 => 10, // EaseInOutCubic -> EaseInOutCubic
        13 => 13, // EaseInOutQuart -> EaseInOutQuart
        14 => 15, // EaseOutQuint  -> EaseOutQuint
        15 => 14, // EaseInQuint   -> EaseInQuint
        16 => 18, // EaseOutExpo   -> EaseOutExpo
        17 => 17, // EaseInExpo    -> EaseInExpo
        18 => 21, // EaseOutCirc   -> EaseOutCirc
        19 => 20, // EaseInCirc    -> EaseInCirc
        20 => 24, // EaseOutBack   -> EaseOutBack
        21 => 23, // EaseInBack    -> EaseInBack
        22 => 22, // EaseInOutCirc -> EaseInOutCirc
        23 => 25, // EaseInOutBack -> EaseInOutBack
        24 => 27, // EaseOutElastic -> EaseOutElastic
        25 => 26, // EaseInElastic  -> EaseInElastic
        26 => 30, // EaseOutBounce  -> EaseOutBounce
        27 => 29, // EaseInBounce   -> EaseInBounce
        28 => 31, // EaseInOutBounce -> EaseInOutBounce
        29 => 28, // EaseInOutElastic -> EaseInOutElastic
        _ => 1 // 未知编号，回退为 Linear
    };

    private static Nrc.Easing ConvertEasing(Rpe.Easing src)
        => new Nrc.Easing(MapEasingNumber((int)src));

    private static Nrc.BpmItem ConvertBpmItem(Rpe.BpmItem src)
    {
        return new Nrc.BpmItem
        {
            Bpm = src.BeatPerMinute,
            StartBeat = new Beat((int[])src.StartBeat)
        };
    }

    private static Nrc.Meta ConvertMeta(Rpe.Meta src)
    {
        return new Nrc.Meta
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
    }

    private static Nrc.JudgeLine ConvertJudgeLine(Rpe.JudgeLine src)
    {
        return new Nrc.JudgeLine
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
    }

    private static Nrc.Note ConvertNote(Rpe.Note src)
    {
        return new Nrc.Note
        {
            Above = src.Above,
            Alpha = src.Alpha,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            IsFake = src.IsFake,
            PositionX = src.PositionX,
            Size = src.Size,
            JudgeArea = src.JudgeArea,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (Nrc.NoteType)(int)src.Type,
            VisibleTime = src.VisibleTime,
            YOffset = src.YOffset,
            Tint = src.Color.ToArray(),
            HitFxColor = src.HitFxColor?.ToArray(),
            HitSound = src.HitSound
        };
    }

    private static Nrc.EventLayer ConvertEventLayer(Rpe.EventLayer src)
    {
        var nrc = new Nrc.EventLayer();
        if (src.MoveXEvents != null)
            nrc.MoveXEvents = src.MoveXEvents.ConvertAll(ConvertFloatEvent);
        if (src.MoveYEvents != null)
            nrc.MoveYEvents = src.MoveYEvents.ConvertAll(ConvertFloatEvent);
        if (src.RotateEvents != null)
            nrc.RotateEvents = src.RotateEvents.ConvertAll(ConvertFloatEvent);
        if (src.AlphaEvents != null)
            nrc.AlphaEvents = src.AlphaEvents.ConvertAll(ConvertIntEvent);
        if (src.SpeedEvents != null)
            nrc.SpeedEvents = src.SpeedEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static Nrc.ExtendLayer ConvertExtendLayer(Rpe.ExtendLayer src)
    {
        if (src == null) return null;
        var nrc = new Nrc.ExtendLayer();
        if (src.ColorEvents != null)
            nrc.ColorEvents = src.ColorEvents.ConvertAll(ConvertByteArrayEvent);
        if (src.ScaleXEvents != null)
            nrc.ScaleXEvents = src.ScaleXEvents.ConvertAll(ConvertFloatEvent);
        if (src.ScaleYEvents != null)
            nrc.ScaleYEvents = src.ScaleYEvents.ConvertAll(ConvertFloatEvent);
        if (src.TextEvents != null)
            nrc.TextEvents = src.TextEvents.ConvertAll(ConvertStringEvent);
        if (src.PaintEvents != null)
            nrc.PaintEvents = src.PaintEvents.ConvertAll(ConvertFloatEvent);
        if (src.GifEvents != null)
            nrc.GifEvents = src.GifEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static Nrc.Event<T> ConvertEvent<T>(Rpe.Event<T> src, System.Func<T, T> valueCopier = null)
    {
        valueCopier ??= v => v;
        return new Nrc.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = src.BezierPoints.ToArray(),
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = ConvertEasing(src.Easing),
            StartValue = valueCopier(src.StartValue),
            EndValue = valueCopier(src.EndValue),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    private static Nrc.Event<float> ConvertFloatEvent(Rpe.Event<float> src) => ConvertEvent(src);
    private static Nrc.Event<int> ConvertIntEvent(Rpe.Event<int> src) => ConvertEvent(src);
    private static Nrc.Event<string> ConvertStringEvent(Rpe.Event<string> src) => ConvertEvent(src);

    private static Nrc.Event<byte[]> ConvertByteArrayEvent(Rpe.Event<byte[]> src) =>
        ConvertEvent(src, v => v?.ToArray());

    private static Nrc.XControl ConvertXControl(Rpe.XControl src) => new Nrc.XControl
    {
        Easing = ConvertEasing(src.Easing),
        X = src.X,
        Pos = src.Pos
    };

    private static Nrc.AlphaControl ConvertAlphaControl(Rpe.AlphaControl src) => new Nrc.AlphaControl
    {
        Easing = ConvertEasing(src.Easing),
        X = src.X,
        Alpha = src.Alpha
    };

    private static Nrc.SizeControl ConvertSizeControl(Rpe.SizeControl src) => new Nrc.SizeControl
    {
        Easing = ConvertEasing(src.Easing),
        X = src.X,
        Size = src.Size
    };

    private static Nrc.SkewControl ConvertSkewControl(Rpe.SkewControl src) => new Nrc.SkewControl
    {
        Easing = ConvertEasing(src.Easing),
        X = src.X,
        Skew = src.Skew
    };

    private static Nrc.YControl ConvertYControl(Rpe.YControl src) => new Nrc.YControl
    {
        Easing = ConvertEasing(src.Easing),
        X = src.X,
        Y = src.Y
    };
}