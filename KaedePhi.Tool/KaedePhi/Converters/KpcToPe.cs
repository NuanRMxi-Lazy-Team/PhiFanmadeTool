using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.KaedePhi.Converters.Utils;
using KaedePhi.Tool.KaedePhi.Events;
using AlphaControl = KaedePhi.Core.KaedePhi.AlphaControl;
using BpmItem = KaedePhi.Core.KaedePhi.BpmItem;
using Chart = KaedePhi.Core.KaedePhi.Chart;
using Easing = KaedePhi.Core.KaedePhi.Easing;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using ExtendLayer = KaedePhi.Core.KaedePhi.ExtendLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using Meta = KaedePhi.Core.KaedePhi.Meta;
using Note = KaedePhi.Core.KaedePhi.Note;
using SizeControl = KaedePhi.Core.KaedePhi.SizeControl;
using SkewControl = KaedePhi.Core.KaedePhi.SkewControl;
using XControl = KaedePhi.Core.KaedePhi.XControl;
using YControl = KaedePhi.Core.KaedePhi.YControl;
using KaedePhi.Tool.KaedePhi.JudgeLines;

namespace KaedePhi.Tool.KaedePhi.Converters;

/// <summary>
///  Kpc格式 -> PhiEdit 格式转换器（框架版）。
/// </summary>
[Obsolete("请改用 KaedePhi.Tool.Converter.PhiEdit.PhiEditConverter.FromKpc()")]
public class KpcToPe
{
    private readonly PhiEditConvertOptions _options;

    public KpcToPe(PhiEditConvertOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// 为兼容既有 PE 偏移定义而保留的常量偏移。
    /// </summary>
    private const int OffsetOffset = 175;

    /// <summary>
    /// 浮点比较容差。
    /// </summary>
    private const float FloatEpsilon = 1e-6f;

    private static readonly CoordinateProfile PeCoordinateProfile = new(
        Pe.Chart.CoordinateSystem.MinX,
        Pe.Chart.CoordinateSystem.MaxX,
        Pe.Chart.CoordinateSystem.MinY,
        Pe.Chart.CoordinateSystem.MaxY,
        Pe.Chart.CoordinateSystem.ClockwiseRotation);


    /// <summary>
    /// 将 Kpc 谱面转换为 PhiEdit 谱面。
    /// </summary>
    /// <param name="src">待转换的Kpc谱面。</param>
    /// <returns>转换后的 PhiEdit 谱面。</returns>
    public Pe.Chart Convert(Chart src)
    {
        ArgumentNullException.ThrowIfNull(src);

        WarnIfUnsupportedMeta(src.Meta);

        return new Pe.Chart
        {
            Offset = src.Meta.Offset + OffsetOffset,
            BpmList = src.BpmList?.ConvertAll(ConvertBpmItem) ?? [],
            JudgeLineList = src.JudgeLineList?.ConvertAll(a => ConvertJudgeLine(a, src.JudgeLineList)) ?? []
        };
    }

    /// <summary>
    /// 将  缓动对象转换为 PE 缓动对象。
    /// </summary>
    private static Pe.Easing ConvertEasing(Easing src)
        => new(KpcToCmdysjEasings.MapEasingNumber((int)src));

    private static float ToPeX(double x) => CoordinateGeometry.ToTargetXf(x, PeCoordinateProfile);
    private static float ToPeY(double y) => CoordinateGeometry.ToTargetYf(y, PeCoordinateProfile);
    private static float ToPeAngle(double angle) => (float)CoordinateGeometry.ToTargetAngle(angle, PeCoordinateProfile);

    /// <summary>
    /// 转换单个 BPM 点。
    /// </summary>
    private static Pe.BpmItem ConvertBpmItem(BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = (float)(double)src.StartBeat
    };

    /// <summary>
    /// 转换单条判定线，并在转换前记录 PE 不支持字段的告警。
    /// </summary>
    private Pe.JudgeLine ConvertJudgeLine(JudgeLine src, List<JudgeLine> allLine)
    {
        WarnIfUnsupportedJudgeLineFields(src);
        var trueSrc = src;
        var pe = new Pe.JudgeLine
        {
            NoteList = trueSrc.Notes?.ConvertAll(ConvertNote) ?? []
        };

        if (!string.Equals(trueSrc.Texture, "line.png", StringComparison.Ordinal) ||
            _options.LineFilter.RemoveTextureLine || trueSrc.AttachUi.HasValue || _options.LineFilter.RemoveAttachUiLine)
        {
            return pe;
        }


        if (trueSrc.Father != -1)
        {
            Warn($"PE 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
            if (_options.FatherLineUnbind.ClassicMode)
            {
                trueSrc = KpcJudgeLineTools.FatherUnbind(allLine.FindIndex(l => l.GetHashCode() == src.GetHashCode()),
                    allLine, _options.FatherLineUnbind.Precision);
            }
            else
                trueSrc = KpcJudgeLineTools.FatherUnbindPlus(
                    allLine.FindIndex(l => l.GetHashCode() == src.GetHashCode()),
                    allLine, _options.FatherLineUnbind.Tolerance);
        }

        ConvertLineEvents(pe, trueSrc.EventLayers ?? []);
        if (pe.AlphaEvents.Count == 0 && pe.AlphaFrames.Count == 0)
            pe.AlphaFrames.Add(new Pe.Frame());

        return pe;
    }

    /// <summary>
    /// 转换单个音符，仅映射 PE 可承载字段。
    /// </summary>
    private static Pe.Note ConvertNote(Note src)
    {
        WarnIfUnsupportedNoteFields(src);

        return new Pe.Note
        {
            Above = src.Above,
            StartBeat = (float)(double)src.StartBeat,
            EndBeat = (float)(double)src.EndBeat,
            IsFake = src.IsFake,
            PositionX = ToPeX(src.PositionX - Chart.CoordinateSystem.MaxX),
            WidthRatio = src.WidthRatio,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (Pe.NoteType)(int)src.Type
        };
    }

    /// <summary>
    /// 将  事件层映射为 PE 的线事件结构。
    /// </summary>
    private void ConvertLineEvents(Pe.JudgeLine target, List<EventLayer> layers)
    {
        if (layers.Count == 0) return;

        var primaryLayer = layers[0];
        for (var i = 1; i < layers.Count; i++)
        {
            if (!HasAnyEventData(layers[i])) continue;
            Warn("JudgeLine 存在多个事件层；PE 仅支持单层，将自动合并为一层");
            // 使用工具将层级合并
            if (_options.MultiLayerMerge.ClassicMode)
            {
                if (_options.MultiLayerMerge.Compress)
                {
                    var layer = Layers.KpcLayerTools.LayerMerge(layers,
                        _options.MultiLayerMerge.Precision);
                    Layers.KpcLayerTools.LayerEventsCompress(layer);
                    primaryLayer = layer;
                }
                else
                    primaryLayer = Layers.KpcLayerTools.LayerMerge(layers,
                        _options.MultiLayerMerge.Precision);
            }
            else
                primaryLayer = Layers.KpcLayerTools.LayerMergePlus(
                    layers,
                    _options.MultiLayerMerge.Precision,
                    _options.MultiLayerMerge.Tolerance);

            break;
        }

        ConvertMoveEvents(target, primaryLayer);
        ConvertScalarEvents(target.RotateFrames, target.RotateEvents, primaryLayer.RotateEvents,
            value => ToPeAngle(value), "旋转");
        ConvertAlphaEvents(target, primaryLayer.AlphaEvents);
        ConvertSpeedFrames(target, primaryLayer.SpeedEvents);
    }

    /// <summary>
    /// 转换 Alpha 事件：PE 的 cf 不支持缓动，需先按单事件切段并压缩，再写入线性事件。
    /// </summary>
    private void ConvertAlphaEvents(Pe.JudgeLine target, List<Kpc.Event<int>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;

        WarnIfEventPayloadUnsupported(sourceEvents, "透明度");

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent =>
            {
                // Slice each source alpha event independently, then compress once before merging.
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
    /// <remarks>
    /// 对每个源事件强制切段；每个切段只产出一个帧：前两个切段取头值，其余取尾值。
    /// </remarks>
    private void ConvertSpeedFrames(Pe.JudgeLine target, List<Kpc.Event<float>>? sourceEvents)
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
    /// <remarks>
    /// 每个输出单元由一个 MoveFrame（立即设定起始位置）和一个 MoveEvent（描述终态及缓动）组成。
    /// XY 事件对齐且缓动一致时直接映射；否则切段线性化输出。
    /// </remarks>
    private void ConvertMoveEvents(Pe.JudgeLine target, EventLayer layer)
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
    /// 处理单个边界区间，选择直接映射或切段线性化策略并输出帧和事件。
    /// </summary>
    private void ProcessMoveInterval(
        Pe.JudgeLine target,
        List<Kpc.Event<double>> xEvents,
        List<Kpc.Event<double>> yEvents,
        float start, float end,
        ref double lastX, ref double lastY)
    {
        var activeX = FindActiveEvent(xEvents, new Beat(start));
        var activeY = FindActiveEvent(yEvents, new Beat(start));

        if (activeX == null && activeY == null) return; // true gap — preserve it

        var xAligned = IsExactlyCovering(activeX, start, end);
        var yAligned = IsExactlyCovering(activeY, start, end);
        var sameEasing = xAligned && yAligned
                                  && activeX != null && activeY != null
                                  && (int)activeX.Easing == (int)activeY.Easing;
        // 只有一轴活跃时（另一轴为 null），无论该轴是否精确覆盖当前区间，均可直接映射。
        // 跨越边界时用 GetValueAtBeat 取正确插值，保留原始缓动曲线，避免不必要的线性化切段。
        var canDirectMap = (activeX != null && activeY == null)
                           || (activeY != null && activeX == null)
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

    /// <summary>
    /// 输出对齐区间的 MoveFrame（立即设定起始状态）和 MoveEvent（描述终态及缓动）。
    /// </summary>
    private static void EmitAlignedMoveSegment(
        Pe.JudgeLine target,
        float start, float end,
        Kpc.Event<double>? activeX, Kpc.Event<double>? activeY,
        ref double lastX, ref double lastY)
    {
        // 对于精确覆盖当前区间的事件，直接取 StartValue/EndValue；
        // 对于跨越边界的事件（如单轴活跃但事件范围超出当前区间），用 GetValueAtBeat 插值，保留原始缓动曲线。
        double startXv, endXv, startYv, endYv;
        if (activeX != null)
        {
            var xAligned = IsExactlyCovering(activeX, start, end);
            startXv = xAligned ? activeX.StartValue : activeX.GetValueAtBeat(new Beat(start));
            endXv = xAligned ? activeX.EndValue : activeX.GetValueAtBeat(new Beat(end));
        }
        else
        {
            startXv = lastX;
            endXv = lastX;
        }
        if (activeY != null)
        {
            var yAligned = IsExactlyCovering(activeY, start, end);
            startYv = yAligned ? activeY.StartValue : activeY.GetValueAtBeat(new Beat(start));
            endYv = yAligned ? activeY.EndValue : activeY.GetValueAtBeat(new Beat(end));
        }
        else
        {
            startYv = lastY;
            endYv = lastY;
        }
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
            XValue = ToPeX(startXv),
            YValue = ToPeY(startYv)
        });
        target.MoveEvents.Add(new Pe.MoveEvent
        {
            StartBeat = start,
            EndBeat = end,
            EasingType = easing,
            EndXValue = ToPeX(endXv),
            EndYValue = ToPeY(endYv)
        });
        lastX = endXv;
        lastY = endYv;
    }

    /// <summary>
    /// 输出 Move 区间不对齐或缓动不一致的告警。
    /// </summary>
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

    /// <summary>
    /// 检查事件是否精确覆盖指定区间（起止拍均在容差内匹配）。
    /// </summary>
    private static bool IsExactlyCovering(Kpc.Event<double>? ev, float start, float end)
        => ev != null
           && Math.Abs((double)ev.StartBeat - start) <= FloatEpsilon
           && Math.Abs((double)ev.EndBeat - end) <= FloatEpsilon;

    /// <summary>
    /// 对 X/Y 不对齐或缓动不一致的区间切割为线性片段，逐段输出 MoveFrame+MoveEvent。
    /// </summary>
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

            // CutEventsInRange 对每个原始事件独立按 cutLength 切割，cutX 与 cutY 的段边界不一定对齐。
            // CollectBoundaries(cutX, cutY) 会产生比任一列表更细的子区间，FindSegment 精确匹配会失败。
            // 改用 FindActiveEvent 在 segStart 处查找活跃段，再插值取区间端点值。
            var xSeg = FindActiveEvent(cutX, new Beat(segStart));
            var ySeg = FindActiveEvent(cutY, new Beat(segStart));

            var startXv = xSeg != null ? xSeg.GetValueAtBeat(new Beat(segStart)) : lastX;
            var endXv = xSeg != null ? xSeg.GetValueAtBeat(new Beat(segEnd)) : lastX;
            var startYv = ySeg != null ? ySeg.GetValueAtBeat(new Beat(segStart)) : lastY;
            var endYv = ySeg != null ? ySeg.GetValueAtBeat(new Beat(segEnd)) : lastY;

            target.MoveFrames.Add(new Pe.MoveFrame
            {
                Beat = segStart,
                XValue = ToPeX(startXv),
                YValue = ToPeY(startYv)
            });
            target.MoveEvents.Add(new Pe.MoveEvent
            {
                StartBeat = segStart,
                EndBeat = segEnd,
                EasingType = 1,
                EndXValue = ToPeX(endXv),
                EndYValue = ToPeY(endYv)
            });

            if (xSeg != null) lastX = endXv;
            if (ySeg != null) lastY = endYv;
        }
    }

    /// <summary>
    /// 转换 double 标量事件通道（如 Rotate）。
    /// </summary>
    private void ConvertScalarEvents(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<Kpc.Event<double>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName)
    {
        ConvertScalarEventsInternal(targetFrames, targetEvents, sourceEvents, valueTransform, channelName);
    }

    /// <summary>
    /// 标量事件转换通用实现。
    /// </summary>
    /// <typeparam name="T"> 标量值类型（float/double/int）。</typeparam>
    /// <param name="targetFrames">输出帧列表。</param>
    /// <param name="targetEvents">输出事件列表；传 <see langword="null"/> 时表示该通道仅导出帧。</param>
    /// <param name="sourceEvents">输入事件列表。</param>
    /// <param name="valueTransform">数值变换（坐标/范围/系数）。</param>
    /// <param name="channelName">通道名，用于告警上下文。</param>
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
                    EasingType = ConvertEasing(ev.Easing),
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


    /// <summary>
    /// 安全转换缓动：若仍出现不支持缓动，降级为线性并告警。
    /// </summary>
    private static int SafeConvertEasingToInt(Easing easing, string context)
    {
        try
        {
            return (int)ConvertEasing(easing);
        }
        catch (KpcToCmdysjEasings.EasingNotSupportedException)
        {
            Warn($"{context}：展开后仍存在不支持的缓动，回退为线性(1)");
            return 1;
        }
    }

    /// <summary>
    /// 对 Move 双精度事件列表进行展开：不支持缓动会切割为线性分段。
    /// </summary>
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

    /// <summary>
    /// 若事件缓动不受支持，切割为多段线性事件拟合曲线。
    /// </summary>
    private List<Kpc.Event<T>> ExpandUnsupportedEasing<T>(Kpc.Event<T> src, string context)
    {
        try
        {
            if (src.IsBezier)
                throw new KpcToCmdysjEasings.EasingNotSupportedException(-1);
            _ = ConvertEasing(src.Easing);
            return [src];
        }
        catch (KpcToCmdysjEasings.EasingNotSupportedException)
        {
            Warn(
                $"{context}：检测到不支持的缓动，将切分为 {(src.EndBeat - src.StartBeat) / _options.Cutting.UnsupportedEasingPrecision} 段线性事件");
            return KpcEventTools.CutEventToLiner(src, 1d / _options.Cutting.UnsupportedEasingPrecision);
        }
    }

    /// <summary>
    /// 收集多个事件列表的起止边界，并去重升序返回。
    /// </summary>
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


    /// <summary>
    /// 在切分后的共享边界上查找与区间完全匹配的事件片段。
    /// </summary>
    private static Kpc.Event<double>? FindSegment(List<Kpc.Event<double>> events, float start, float end)
    {
        return events.FirstOrDefault(e =>
            Math.Abs((double)e.StartBeat - start) <= FloatEpsilon
            && Math.Abs((double)e.EndBeat - end) <= FloatEpsilon);
    }

    private static float ToSingle<T>(T value) => value switch
    {
        float v => v,
        double v => (float)v,
        int v => v,
        _ => throw new NotSupportedException($"Unsupported scalar event value type: {typeof(T).Name}")
    };

    private static bool HasAnyEventData(EventLayer layer)
        => (layer.MoveXEvents?.Count ?? 0) > 0
           || (layer.MoveYEvents?.Count ?? 0) > 0
           || (layer.RotateEvents?.Count ?? 0) > 0
           || (layer.AlphaEvents?.Count ?? 0) > 0
           || (layer.SpeedEvents?.Count ?? 0) > 0;

    /// <summary>
    /// 检查 Meta 中 PE 不支持的字段是否出现非默认值，并逐项告警。
    /// </summary>
    private static void WarnIfUnsupportedMeta(Meta src)
    {
        var defaults = new Meta();
        if (src.Background != defaults.Background)
            Warn($"PE 不支持 Meta.Background（值='{src.Background}'）");
        if (src.Author != defaults.Author) Warn($"PE 不支持 Meta.Author（值='{src.Author}'）");
        if (src.Composer != defaults.Composer) Warn($"PE 不支持 Meta.Composer（值='{src.Composer}'）");
        if (src.Artist != defaults.Artist) Warn($"PE 不支持 Meta.Artist（值='{src.Artist}'）");
        if (src.Level != defaults.Level) Warn($"PE 不支持 Meta.Level（值='{src.Level}'）");
        if (src.Name != defaults.Name) Warn($"PE 不支持 Meta.Name（值='{src.Name}'）");
        if (src.Song != defaults.Song) Warn($"PE 不支持 Meta.Song（值='{src.Song}'）");
    }

    /// <summary>
    /// 检查判定线中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private void WarnIfUnsupportedJudgeLineFields(JudgeLine src)
    {
        if (!string.Equals(src.Name, "Untitled", StringComparison.Ordinal))
            Warn($"PE 不支持 JudgeLine.Name（值='{src.Name}'）");
        if (!string.Equals(src.Texture, "line.png", StringComparison.Ordinal))
            Warn(
                $"PE 不支持 JudgeLine.Texture（值='{src.Texture}'），{(_options.LineFilter.RemoveTextureLine ? "，判定线将被自动移除。" : "。")}");
        if (!IsDefaultAnchor(src.Anchor))
            Warn($"PE 不支持 JudgeLine.Anchor（值='[{string.Join(", ", src.Anchor)}]'）");
        if (src.Father != -1)
            Warn($"PE 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
        if (!src.IsCover)
            Warn($"PE 不支持 JudgeLine.IsCover（值={src.IsCover}）");
        if (src.ZOrder != 0)
            Warn($"PE 不支持 JudgeLine.ZOrder（值={src.ZOrder}）");
        if (src.AttachUi.HasValue)
            Warn(
                $"PE 不支持 JudgeLine.AttachUi（值={(int)src.AttachUi.Value}）{(_options.LineFilter.RemoveAttachUiLine ? "，判定线将被自动移除。" : "。")}"
            );
        if (src.IsGif)
            Warn($"PE 不支持 JudgeLine.IsGif（值={src.IsGif}）");
        if (Math.Abs(src.BpmFactor - 1f) > FloatEpsilon)
            Warn($"PE 不支持 JudgeLine.BpmFactor（值={src.BpmFactor}）");

        if (HasNonDefaultExtendLayer(src.Extended))
            Warn("PE 不支持 JudgeLine.Extended（包含非默认数据）");
        if (!IsDefaultXControls(src.PositionControls))
            Warn("PE 不支持 JudgeLine.PositionControls（包含非默认数据）");
        if (!IsDefaultAlphaControls(src.AlphaControls))
            Warn("PE 不支持 JudgeLine.AlphaControls（包含非默认数据）");
        if (!IsDefaultSizeControls(src.SizeControls))
            Warn("PE 不支持 JudgeLine.SizeControls（包含非默认数据）");
        if (!IsDefaultSkewControls(src.SkewControls))
            Warn("PE 不支持 JudgeLine.SkewControls（包含非默认数据）");
        if (!IsDefaultYControls(src.YControls))
            Warn("PE 不支持 JudgeLine.YControls（包含非默认数据）");
    }

    /// <summary>
    /// 检查音符中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private static void WarnIfUnsupportedNoteFields(Note src)
    {
        if (src.Alpha != 255)
            Warn($"PE 不支持 Note.Alpha（值={src.Alpha}）");
        if (Math.Abs(src.JudgeArea - 1f) > FloatEpsilon)
            Warn($"PE 不支持 Note.JudgeArea（值={src.JudgeArea}）");
        if (Math.Abs(src.VisibleTime - 999999f) > FloatEpsilon)
            Warn($"PE 不支持 Note.VisibleTime（值={src.VisibleTime}）");
        if (Math.Abs(src.YOffset) > FloatEpsilon)
            Warn($"PE 不支持 Note.YOffset（值={src.YOffset}）");
        if (!IsDefaultTint(src.Tint))
            Warn($"PE 不支持 Note.Tint（值='[{string.Join(", ", src.Tint)}]'）");
        if (src.HitFxColor != null)
            Warn($"PE 不支持 Note.HitFxColor（值='[{string.Join(", ", src.HitFxColor)}]'）");
        if (!string.IsNullOrWhiteSpace(src.HitSound))
            Warn($"PE 不支持 Note.HitSound（值='{src.HitSound}'）");
    }

    /// <summary>
    /// 检查  事件载荷中 PE 无法表达的信息，并输出告警。
    /// </summary>
    private static void WarnIfEventPayloadUnsupported<T>(IEnumerable<Kpc.Event<T>> events, string channel)
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

    private static bool HasNonDefaultExtendLayer(ExtendLayer? layer)
        => layer != null
           && ((layer.ColorEvents?.Count ?? 0) > 0
               || (layer.ScaleXEvents?.Count ?? 0) > 0
               || (layer.ScaleYEvents?.Count ?? 0) > 0
               || (layer.TextEvents?.Count ?? 0) > 0
               || (layer.PaintEvents?.Count ?? 0) > 0
               || (layer.GifEvents?.Count ?? 0) > 0);

    private static bool IsDefaultAnchor(float[]? anchor)
        => anchor is { Length: 2 }
           && Math.Abs(anchor[0] - 0.5f) <= FloatEpsilon
           && Math.Abs(anchor[1] - 0.5f) <= FloatEpsilon;

    private static bool IsDefaultTint(byte[]? tint)
        => tint is [255, 255, 255];

    private static bool IsDefaultBezierPoints(float[]? points)
        => points is { Length: 4 }
           && Math.Abs(points[0]) <= FloatEpsilon
           && Math.Abs(points[1]) <= FloatEpsilon
           && Math.Abs(points[2]) <= FloatEpsilon
           && Math.Abs(points[3]) <= FloatEpsilon;

    private static bool IsDefaultXControls(List<XControl>? controls)
        => XControl.Default.Equals(controls);

    private static bool IsDefaultAlphaControls(List<AlphaControl>? controls)
        => AlphaControl.Default.Equals(controls);

    private static bool IsDefaultSizeControls(List<SizeControl>? controls)
        => SizeControl.Default.Equals(controls);

    private static bool IsDefaultSkewControls(List<SkewControl>? controls)
        => SkewControl.Default.Equals(controls);

    private static bool IsDefaultYControls(List<YControl>? controls)
        => YControl.Default.Equals(controls);

    /// <summary>
    /// 输出 ToPe 统一前缀的告警日志。
    /// </summary>
    private static void Warn(string message) => KpcToolLog.OnWarning($"[ToPe] {message}");
}