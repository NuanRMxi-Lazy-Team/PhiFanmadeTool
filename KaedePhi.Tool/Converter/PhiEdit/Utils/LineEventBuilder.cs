using KaedePhi.Core.Common;
using global::KaedePhi.Tool.KaedePhi.Converters.Utils;
using global::KaedePhi.Tool.KaedePhi.Events;
using global::KaedePhi.Tool.KaedePhi;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KpcEasing = KaedePhi.Core.KaedePhi.Easing;
using KpcEventLayer = KaedePhi.Core.KaedePhi.EventLayer;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// KPC 事件到 PE Frame/Event 的构建器。
/// </summary>
public class LineEventBuilder
{
    private const float FloatEpsilon = 1e-6f;
    private readonly PhiEditConvertOptions _options;

    public LineEventBuilder(PhiEditConvertOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// 将 KPC 事件层映射为 PE 的线事件结构。
    /// </summary>
    public void ConvertLineEvents(Pe.JudgeLine target, List<KpcEventLayer> layers)
    {
        if (layers.Count == 0) return;

        var primaryLayer = layers[0];
        for (var i = 1; i < layers.Count; i++)
        {
            if (!HasAnyEventData(layers[i])) continue;
            Warn("JudgeLine 存在多个事件层；PE 仅支持单层，将自动合并为一层");
            if (_options.MultiLayerMerge.ClassicMode)
            {
                if (_options.MultiLayerMerge.Compress)
                {
                    var layer = global::KaedePhi.Tool.KaedePhi.Layers.KpcLayerTools.LayerMerge(layers,
                        _options.MultiLayerMerge.Precision);
                    global::KaedePhi.Tool.KaedePhi.Layers.KpcLayerTools.LayerEventsCompress(layer);
                    primaryLayer = layer;
                }
                else
                    primaryLayer = global::KaedePhi.Tool.KaedePhi.Layers.KpcLayerTools.LayerMerge(layers,
                        _options.MultiLayerMerge.Precision);
            }
            else
                primaryLayer = global::KaedePhi.Tool.KaedePhi.Layers.KpcLayerTools.LayerMergePlus(
                    layers,
                    _options.MultiLayerMerge.Precision,
                    _options.MultiLayerMerge.Tolerance);

            break;
        }

        ConvertMoveEvents(target, primaryLayer);
        ConvertScalarEvents(target.RotateFrames, target.RotateEvents, primaryLayer.RotateEvents,
            value => Transform.TransformToPeAngle(value), "旋转");
        ConvertAlphaEvents(target, primaryLayer.AlphaEvents);
        ConvertSpeedFrames(target, primaryLayer.SpeedEvents);
    }

    /// <summary>
    /// 转换 Alpha 事件：PE 的 cf 不支持缓动，需先按单事件切段并压缩，再写入线性事件。
    /// </summary>
    public void ConvertAlphaEvents(Pe.JudgeLine target, List<Kpc.Event<int>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;

        WarnIfEventPayloadUnsupported(sourceEvents, "透明度");

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent =>
            {
                var sliced = KpcEventTools.CutEventToLiner(srcEvent, 1d / _options.Alpha.CutPrecision);
                return _options.Alpha.CutCompress
                    ? KpcEventTools.EventListCompressSlope(sliced, _options.Alpha.CutTolerance)
                    : sliced;
            })
            .ToList();

        var previousEndBeat = float.MinValue;
        var previousEndValue = float.NaN;

        foreach (var ev in ordered)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            var startValue = ToSingle(ev.StartValue);
            var endValue = ToSingle(ev.EndValue);

            var disconnected = Math.Abs(previousEndBeat - startBeat) > FloatEpsilon;
            var changed = float.IsNaN(previousEndValue) || Math.Abs(previousEndValue - startValue) > FloatEpsilon;
            if (disconnected || changed)
            {
                target.AlphaFrames.Add(new Pe.Frame
                {
                    Beat = startBeat,
                    Value = startValue
                });
            }

            target.AlphaEvents.Add(new Pe.Event
            {
                StartBeat = startBeat,
                EndBeat = endBeat,
                EasingType = new Pe.Easing(1),
                EndValue = endValue
            });

            previousEndBeat = endBeat;
            previousEndValue = endValue;
        }
    }

    /// <summary>
    /// 转换 Speed 事件：PE 无速度事件，仅导出帧。
    /// </summary>
    public void ConvertSpeedFrames(Pe.JudgeLine target, List<Kpc.Event<float>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;

        WarnIfEventPayloadUnsupported(sourceEvents, "速度");

        foreach (var srcEvent in sourceEvents.OrderBy(e => (double)e.StartBeat))
        {
            var slices = KpcEventTools.CutEventToLiner(srcEvent, 1d / _options.Speed.CutPrecision);
            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var useStartValue = i < 2;
                var beat = useStartValue ? (float)(double)slice.StartBeat : (float)(double)slice.EndBeat;
                var value = (useStartValue ? ToSingle(slice.StartValue) : ToSingle(slice.EndValue)) * (14f / 9f);

                target.SpeedFrames.Add(new Pe.Frame
                {
                    Beat = beat,
                    Value = value
                });
            }
        }
    }

    /// <summary>
    /// 转换 MoveX/MoveY 事件为 PE MoveFrame 与 MoveEvent。
    /// </summary>
    public void ConvertMoveEvents(Pe.JudgeLine target, KpcEventLayer layer)
    {
        var xEvents = ExpandEventsForUnsupportedEasing(layer.MoveXEvents ?? [], "移动X");
        var yEvents = ExpandEventsForUnsupportedEasing(layer.MoveYEvents ?? [], "移动Y");
        if (xEvents.Count == 0 && yEvents.Count == 0) return;

        WarnIfEventPayloadUnsupported(xEvents, "移动X");
        WarnIfEventPayloadUnsupported(yEvents, "移动Y");

        var boundaries = CollectBoundaries(xEvents, yEvents);
        if (boundaries.Count < 2) return;

        var lastX = 0d;
        var lastY = 0d;

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var start = boundaries[i];
            var end = boundaries[i + 1];
            if (end - start <= FloatEpsilon) continue;
            ProcessMoveInterval(target, xEvents, yEvents, start, end, ref lastX, ref lastY);
        }
    }

    /// <summary>
    /// 转换 double 标量事件通道（如 Rotate）。
    /// </summary>
    public void ConvertScalarEvents(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<Kpc.Event<double>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName)
    {
        ConvertScalarEventsInternal(targetFrames, targetEvents, sourceEvents, valueTransform, channelName);
    }

    #region Move Event Helpers

    private void ProcessMoveInterval(
        Pe.JudgeLine target,
        List<Kpc.Event<double>> xEvents,
        List<Kpc.Event<double>> yEvents,
        float start, float end,
        ref double lastX, ref double lastY)
    {
        var activeX = FindActiveEvent(xEvents, new Beat(start));
        var activeY = FindActiveEvent(yEvents, new Beat(start));

        if (activeX == null && activeY == null) return;

        var xAligned = IsExactlyCovering(activeX, start, end);
        var yAligned = IsExactlyCovering(activeY, start, end);
        var sameEasing = xAligned && yAligned
                                  && activeX != null && activeY != null
                                  && (int)activeX.Easing == (int)activeY.Easing;
        var canDirectMap = (xAligned && activeY == null)
                           || (yAligned && activeX == null)
                           || sameEasing;

        if (canDirectMap)
        {
            EmitAlignedMoveSegment(target, start, end, activeX, activeY, ref lastX, ref lastY);
        }
        else
        {
            WarnMoveSegmentMisalignment(activeX, activeY, xAligned, yAligned, start, end);
            EmitCutMoveSegments(target, xEvents, yEvents, start, end, ref lastX, ref lastY);
        }
    }

    private static void EmitAlignedMoveSegment(
        Pe.JudgeLine target,
        float start, float end,
        Kpc.Event<double>? activeX, Kpc.Event<double>? activeY,
        ref double lastX, ref double lastY)
    {
        var startXv = activeX?.StartValue ?? lastX;
        var endXv = activeX?.EndValue ?? lastX;
        var startYv = activeY?.StartValue ?? lastY;
        var endYv = activeY?.EndValue ?? lastY;
        int easing;
        if (activeX != null)
            easing = SafeConvertEasingToInt(activeX.Easing, $"移动@{start:F3}");
        else if (activeY != null)
            easing = SafeConvertEasingToInt(activeY.Easing, $"移动@{start:F3}");
        else
            easing = 1;

        target.MoveFrames.Add(new Pe.MoveFrame
        {
            Beat = start,
            XValue = Transform.TransformToPeX(startXv),
            YValue = Transform.TransformToPeY(startYv)
        });
        target.MoveEvents.Add(new Pe.MoveEvent
        {
            StartBeat = start,
            EndBeat = end,
            EasingType = easing,
            EndXValue = Transform.TransformToPeX(endXv),
            EndYValue = Transform.TransformToPeY(endYv)
        });
        lastX = endXv;
        lastY = endYv;
    }

    private static void WarnMoveSegmentMisalignment(
        Kpc.Event<double>? activeX, Kpc.Event<double>? activeY,
        bool xAligned, bool yAligned,
        float start, float end)
    {
        switch (xAligned)
        {
            case false when !yAligned:
                Warn($"Move 区间 [{start:F3}, {end:F3}] X/Y 事件均未完整覆盖此区间，将切段线性化");
                break;
            case false:
                Warn($"Move 区间 [{start:F3}, {end:F3}] X 事件跨越 Y 边界（未对齐），将切段线性化");
                break;
            default:
            {
                if (!yAligned)
                    Warn($"Move 区间 [{start:F3}, {end:F3}] Y 事件跨越 X 边界（未对齐），将切段线性化");
                else
                {
                    var xEasingNum = activeX != null ? (int)activeX.Easing : 0;
                    var yEasingNum = activeY != null ? (int)activeY.Easing : 0;
                    Warn($"Move 区间 [{start:F3}, {end:F3}] X/Y 缓动类型不一致（X={xEasingNum}, Y={yEasingNum}），将切段线性化");
                }

                break;
            }
        }
    }

    private static bool IsExactlyCovering(Kpc.Event<double>? ev, float start, float end)
        => ev != null
           && Math.Abs((double)ev.StartBeat - start) <= FloatEpsilon
           && Math.Abs((double)ev.EndBeat - end) <= FloatEpsilon;

    private void EmitCutMoveSegments(
        Pe.JudgeLine target,
        List<Kpc.Event<double>> xEvents,
        List<Kpc.Event<double>> yEvents,
        float start, float end,
        ref double lastX, ref double lastY)
    {
        var startBeat = new Beat(start);
        var endBeat = new Beat(end);
        var cutLength = 1d / _options.Cutting.MisalignedXyEventPrecision;

        var cutX = KpcEventTools.CutEventsInRange(xEvents, startBeat, endBeat, cutLength);
        var cutY = KpcEventTools.CutEventsInRange(yEvents, startBeat, endBeat, cutLength);

        var subBoundaries = CollectBoundaries(cutX, cutY);
        subBoundaries.Add(start);
        subBoundaries.Add(end);
        subBoundaries = subBoundaries.Distinct().OrderBy(v => v).ToList();

        for (var i = 0; i < subBoundaries.Count - 1; i++)
        {
            var segStart = subBoundaries[i];
            var segEnd = subBoundaries[i + 1];
            if (segEnd - segStart <= FloatEpsilon) continue;

            var xSeg = FindSegment(cutX, segStart, segEnd);
            var ySeg = FindSegment(cutY, segStart, segEnd);

            var startXv = xSeg?.StartValue ?? lastX;
            var endXv = xSeg?.EndValue ?? lastX;
            var startYv = ySeg?.StartValue ?? lastY;
            var endYv = ySeg?.EndValue ?? lastY;

            target.MoveFrames.Add(new Pe.MoveFrame
            {
                Beat = segStart,
                XValue = Transform.TransformToPeX(startXv),
                YValue = Transform.TransformToPeY(startYv)
            });
            target.MoveEvents.Add(new Pe.MoveEvent
            {
                StartBeat = segStart,
                EndBeat = segEnd,
                EasingType = 1,
                EndXValue = Transform.TransformToPeX(endXv),
                EndYValue = Transform.TransformToPeY(endYv)
            });

            if (xSeg != null) lastX = xSeg.EndValue;
            if (ySeg != null) lastY = ySeg.EndValue;
        }
    }

    #endregion

    #region Scalar Event Helpers

    private void ConvertScalarEventsInternal<T>(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<Kpc.Event<T>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;
        WarnIfEventPayloadUnsupported(sourceEvents, channelName);

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent => ExpandUnsupportedEasing(srcEvent, $"{channelName}@{(double)srcEvent.StartBeat:F3}"))
            .ToList();

        var previousEndBeat = float.MinValue;
        var previousEndValue = float.NaN;

        foreach (var ev in ordered)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            var startValue = valueTransform(ToSingle(ev.StartValue));
            var endValue = valueTransform(ToSingle(ev.EndValue));

            var disconnected = Math.Abs(previousEndBeat - startBeat) > FloatEpsilon;
            var changed = float.IsNaN(previousEndValue) || Math.Abs(previousEndValue - startValue) > FloatEpsilon;
            if (disconnected || changed)
            {
                targetFrames.Add(new Pe.Frame
                {
                    Beat = startBeat,
                    Value = startValue
                });
            }

            if (targetEvents != null)
            {
                targetEvents.Add(new Pe.Event
                {
                    StartBeat = startBeat,
                    EndBeat = endBeat,
                    EasingType = EasingConverter.ConvertEasing(ev.Easing),
                    EndValue = endValue
                });
            }
            else
            {
                targetFrames.Add(new Pe.Frame
                {
                    Beat = endBeat,
                    Value = endValue
                });
            }

            previousEndBeat = endBeat;
            previousEndValue = endValue;
        }
    }

    private List<Kpc.Event<double>> ExpandEventsForUnsupportedEasing(
        List<Kpc.Event<double>> source,
        string channel)
    {
        var expanded = new List<Kpc.Event<double>>();
        foreach (var ev in source.OrderBy(e => (double)e.StartBeat))
        {
            expanded.AddRange(ExpandUnsupportedEasing(ev, $"{channel}@{(double)ev.StartBeat:F3}"));
        }

        return expanded;
    }

    private List<Kpc.Event<T>> ExpandUnsupportedEasing<T>(Kpc.Event<T> src, string context)
    {
        try
        {
            if (src.IsBezier)
                throw new EasingConverter.EasingNotSupportedException(-1);
            _ = EasingConverter.ConvertEasing(src.Easing);
            return [src];
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            Warn(
                $"{context}：检测到不支持的缓动，将切分为 {src.EndBeat - src.StartBeat / _options.Cutting.UnsupportedEasingPrecision} 段线性事件");
            return KpcEventTools.CutEventToLiner(src, 1d / _options.Cutting.UnsupportedEasingPrecision);
        }
    }

    #endregion

    #region Common Helpers

    private static List<float> CollectBoundaries(params List<Kpc.Event<double>>[] eventLists)
    {
        var boundaries = new SortedSet<float>();
        foreach (var list in eventLists)
        {
            foreach (var ev in list)
            {
                boundaries.Add((float)(double)ev.StartBeat);
                boundaries.Add((float)(double)ev.EndBeat);
            }
        }

        return boundaries.ToList();
    }

    private static Kpc.Event<double>? FindActiveEvent(List<Kpc.Event<double>> events, Beat beat)
    {
        var beatValue = (double)beat;
        return events.FirstOrDefault(e =>
            beatValue + FloatEpsilon >= (double)e.StartBeat
            && beatValue < (double)e.EndBeat - FloatEpsilon);
    }

    private static Kpc.Event<double>? FindSegment(List<Kpc.Event<double>> events, float start, float end)
    {
        return events.FirstOrDefault(e =>
            Math.Abs((double)e.StartBeat - start) <= FloatEpsilon
            && Math.Abs((double)e.EndBeat - end) <= FloatEpsilon);
    }

    private static int SafeConvertEasingToInt(KpcEasing easing, string context)
    {
        try
        {
            return (int)EasingConverter.ConvertEasing(easing);
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            Warn($"{context}：展开后仍存在不支持的缓动，回退为线性(1)");
            return 1;
        }
    }

    private static float ToSingle<T>(T value) => value switch
    {
        float v => v,
        double v => (float)v,
        int v => v,
        _ => throw new NotSupportedException($"Unsupported scalar event value type: {typeof(T).Name}")
    };

    private static bool HasAnyEventData(KpcEventLayer layer)
        => (layer.MoveXEvents?.Count ?? 0) > 0
           || (layer.MoveYEvents?.Count ?? 0) > 0
           || (layer.RotateEvents?.Count ?? 0) > 0
           || (layer.AlphaEvents?.Count ?? 0) > 0
           || (layer.SpeedEvents?.Count ?? 0) > 0;

    private void WarnIfEventPayloadUnsupported<T>(IEnumerable<Kpc.Event<T>> events, string channel)
    {
        foreach (var e in events)
        {
            if (e.IsBezier)
                Warn($"{channel}：Bezier 事件不受 PE 原生事件模型支持，事件将被自动转换为线性事件");
            if (!IsDefaultBezierPoints(e.BezierPoints))
                Warn($"{channel}：非默认 BezierPoints 将被丢弃");
            if (Math.Abs(e.EasingLeft) > FloatEpsilon || Math.Abs(e.EasingRight - 1f) > FloatEpsilon)
                Warn($"{channel}：PE 不支持 EasingLeft/EasingRight 裁剪，事件将被自动转换为线性事件");
            if (!string.IsNullOrWhiteSpace(e.Font))
                Warn($"{channel}：PE 不支持 Font 字段，字段将被丢弃");
        }
    }

    private static bool IsDefaultBezierPoints(float[]? points)
        => points is { Length: 4 }
           && Math.Abs(points[0]) <= FloatEpsilon
           && Math.Abs(points[1]) <= FloatEpsilon
           && Math.Abs(points[2]) <= FloatEpsilon
           && Math.Abs(points[3]) <= FloatEpsilon;

    private static void Warn(string message) => KpcToolLog.OnWarning($"[ToPe] {message}");

    #endregion
}
