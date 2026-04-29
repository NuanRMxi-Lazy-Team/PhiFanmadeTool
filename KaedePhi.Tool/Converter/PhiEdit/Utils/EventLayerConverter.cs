using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class EventLayerConverter
{
    /// <summary>
    /// 将 PE 判定线上的各通道帧/事件规范化为 KPC 事件层。
    /// </summary>
    public static Kpc.EventLayer ConvertEventLayer(Pe.JudgeLine src, double horizonBeat) => new()
    {
        MoveXEvents = FrameEventInterpolator.BuildMoveAxisEvents(
            src.MoveFrames, src.MoveEvents, horizonBeat, point => point.X, Transform.TransformToKpcX),
        MoveYEvents = FrameEventInterpolator.BuildMoveAxisEvents(
            src.MoveFrames, src.MoveEvents, horizonBeat, point => point.Y, Transform.TransformToKpcY),
        RotateEvents = FrameEventInterpolator.BuildScalarEvents(
            src.RotateFrames, src.RotateEvents, horizonBeat, Transform.TransformToKpcAngle),
        AlphaEvents = FrameEventInterpolator.BuildScalarEvents(
            src.AlphaFrames, src.AlphaEvents, horizonBeat,
            value => Math.Clamp((int)Math.Round(value), 0, 255)),
        SpeedEvents = FrameEventInterpolator.BuildScalarEvents(
            src.SpeedFrames, [], horizonBeat, value => (float)(value / (14d / 9d)))
    };
}
