using PhiFanmade.Core.Common;
using PhiFanmade.Tool.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Converters.Utils;
using PhiFanmade.Tool.PhiFanmadeNrc.Events;
using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Converters;

/// <summary>
/// NRC 格式 -> PhiEdit 格式转换器（框架版）。
/// </summary>
public static class NrcToPe
{
    /// <summary>
    /// 与 NrcToRpe 保持一致：不支持缓动切段拟合时的切割精度。
    /// </summary>
    private const int UnsupportedEasingCutPrecision = 64;

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
    /// 将 NRC 谱面转换为 PhiEdit 谱面。
    /// </summary>
    /// <param name="nrc">待转换的 NRC 谱面。</param>
    /// <returns>转换后的 PhiEdit 谱面。</returns>
    public static Pe.Chart Convert(Nrc.Chart nrc)
    {
        ArgumentNullException.ThrowIfNull(nrc);

        WarnIfUnsupportedMeta(nrc.Meta);

        return new Pe.Chart
        {
            Offset = nrc.Meta.Offset + OffsetOffset,
            BpmList = nrc.BpmList?.ConvertAll(ConvertBpmItem) ?? [],
            JudgeLineList = nrc.JudgeLineList?.ConvertAll(a => ConvertJudgeLine(a, nrc.JudgeLineList)) ?? []
        };
    }

    /// <summary>
    /// 将 NRC 缓动对象转换为 PE 缓动对象。
    /// </summary>
    private static Pe.Easing ConvertEasing(Nrc.Easing src)
        => new(NrcToCmdysjEasings.MapEasingNumber((int)src));

    private static float ToPeX(double x) => CoordinateGeometry.ToTargetXf(x, PeCoordinateProfile);
    private static float ToPeY(double y) => CoordinateGeometry.ToTargetYf(y, PeCoordinateProfile);
    private static float ToPeAngle(double angle) => (float)CoordinateGeometry.ToTargetAngle(angle, PeCoordinateProfile);

    /// <summary>
    /// 转换单个 BPM 点。
    /// </summary>
    private static Pe.BpmItem ConvertBpmItem(Nrc.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = (float)(double)src.StartBeat
    };

    /// <summary>
    /// 转换单条判定线，并在转换前记录 PE 不支持字段的告警。
    /// </summary>
    private static Pe.JudgeLine ConvertJudgeLine(Nrc.JudgeLine src, List<Nrc.JudgeLine> allLine)
    {
        WarnIfUnsupportedJudgeLineFields(src);
        var trueSrc = src;
        var pe = new Pe.JudgeLine
        {
            NoteList = trueSrc.Notes?.ConvertAll(ConvertNote) ?? []
        };
        if (trueSrc.Father != -1)
        {
            Warn($"JudgeLine.Father is unsupported by PE (value={src.Father}), will be auto unbind");
            trueSrc = NrcJudgeLineTools.FatherUnbindPlus(allLine.FindIndex(l => l.GetHashCode() == src.GetHashCode()),
                allLine);
        }

        ConvertLineEvents(pe, trueSrc.EventLayers ?? []);
        return pe;
    }

    /// <summary>
    /// 转换单个音符，仅映射 PE 可承载字段。
    /// </summary>
    private static Pe.Note ConvertNote(Nrc.Note src)
    {
        WarnIfUnsupportedNoteFields(src);

        return new Pe.Note
        {
            Above = src.Above,
            StartBeat = (float)(double)src.StartBeat,
            EndBeat = (float)(double)src.EndBeat,
            IsFake = src.IsFake,
            PositionX = ToPeX(src.PositionX - Nrc.Chart.CoordinateSystem.MaxX),
            WidthRatio = src.Size,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (Pe.NoteType)(int)src.Type
        };
    }

    /// <summary>
    /// 将 NRC 事件层映射为 PE 的线事件结构。
    /// </summary>
    /// <remarks>
    /// PE 原生仅支持一层线事件：当输入存在多层时仅保留第一层，并对后续非空层输出警告。
    /// </remarks>
    private static void ConvertLineEvents(Pe.JudgeLine target, List<Nrc.EventLayer> layers)
    {
        if (layers.Count == 0) return;

        var primaryLayer = layers[0];
        for (var i = 1; i < layers.Count; i++)
        {
            if (!HasAnyEventData(layers[i])) continue;
            Warn("JudgeLine has multiple event layers; PE supports one layer only, will auto merge to one");
            // 使用Nrc工具将层级合并
            primaryLayer = Layers.NrcLayerTools.LayerMergePlus(layers);
            break;
        }

        ConvertMoveEvents(target, primaryLayer);
        ConvertScalarEvents(target.RotateFrames, target.RotateEvents, primaryLayer.RotateEvents,
            value => ToPeAngle(value), "Rotate");
        ConvertAlphaEvents(target, primaryLayer.AlphaEvents);
        ConvertSpeedFrames(target, primaryLayer.SpeedEvents);
    }

    /// <summary>
    /// 转换 Alpha 事件：PE 的 cf 不支持缓动，需先按单事件切段并压缩，再写入线性事件。
    /// </summary>
    private static void ConvertAlphaEvents(Pe.JudgeLine target, List<Nrc.Event<int>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;

        WarnIfNrcEventPayloadUnsupported(sourceEvents, "Alpha");

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent =>
            {
                // Slice each source alpha event independently, then compress once before merging.
                var sliced = NrcEventTools.CutEventToLiner(srcEvent, 1 / 64d);
                return NrcEventTools.EventListCompress(sliced);
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
    private static void ConvertSpeedFrames(Pe.JudgeLine target, List<Nrc.Event<float>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;

        WarnIfNrcEventPayloadUnsupported(sourceEvents, "Speed");

        foreach (var srcEvent in sourceEvents.OrderBy(e => (double)e.StartBeat))
        {
            var slices = CutEventToLinear(srcEvent);
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
    /// 通过收集 X/Y 边界做切片，区间末值写入 MoveEvent；首边界写入初始 MoveFrame。
    /// </remarks>
    private static void ConvertMoveEvents(Pe.JudgeLine target, Nrc.EventLayer layer)
    {
        var xEvents = NrcEventTools.EventListCompress(
            ExpandEventsForUnsupportedEasing(layer.MoveXEvents ?? [], "MoveX"), 0d);
        var yEvents = NrcEventTools.EventListCompress(
            ExpandEventsForUnsupportedEasing(layer.MoveYEvents ?? [], "MoveY"), 0d);
        if (xEvents.Count == 0 && yEvents.Count == 0) return;

        WarnIfNrcEventPayloadUnsupported(xEvents, "MoveX");
        WarnIfNrcEventPayloadUnsupported(yEvents, "MoveY");

        var boundaries = CollectBoundaries(xEvents, yEvents);
        if (boundaries.Count < 2) return;

        var firstBeat = new Beat(boundaries[0]);
        target.MoveFrames.Add(new Pe.MoveFrame
        {
            Beat = boundaries[0],
            XValue = ToPeX(GetValueAt(xEvents, firstBeat)),
            YValue = ToPeY(GetValueAt(yEvents, firstBeat))
        });

        var hasPreviousOutputInterval = false;
        var previousOutputEnd = 0f;
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var start = boundaries[i];
            var end = boundaries[i + 1];
            if (end - start <= FloatEpsilon) continue;

            var emitted = ProcessMoveInterval(target, xEvents, yEvents, start, end);
            if (!emitted) continue;

            // If there is a real gap between two emitted intervals, insert a frame to prevent implicit chaining.
            if (hasPreviousOutputInterval && start - previousOutputEnd > FloatEpsilon)
            {
                var startBeat = new Beat(start);
                target.MoveFrames.Add(new Pe.MoveFrame
                {
                    Beat = start,
                    XValue = ToPeX(GetValueAt(xEvents, startBeat)),
                    YValue = ToPeY(GetValueAt(yEvents, startBeat))
                });
            }

            hasPreviousOutputInterval = true;
            previousOutputEnd = end;
        }
    }

    /// <summary>
    /// 处理单个 Move 区间并选择直接映射或切段线性化策略。
    /// </summary>
    private static bool ProcessMoveInterval(
        Pe.JudgeLine target,
        List<Nrc.Event<double>> xEvents,
        List<Nrc.Event<double>> yEvents,
        float start,
        float end)
    {
        var startBeat = new Beat(start);
        var endBeat = new Beat(end);
        var activeX = FindActiveEvent(xEvents, startBeat);
        var activeY = FindActiveEvent(yEvents, startBeat);

        if (activeX == null && activeY == null)
        {
            // Keep true gaps empty instead of fabricating constant-velocity move events.
            return false;
        }

        if (activeX != null && activeY != null
                            && IsEventExactMatch(activeX, activeY, start, end)
                            && (int)activeX.Easing == (int)activeY.Easing)
        {
            AppendMoveEvent(target, xEvents, yEvents, start, end,
                SafeConvertEasingToInt(activeX.Easing, $"Move@{start:F3}"));
            return true;
        }

        var hasX = activeX != null;
        var hasY = activeY != null;
        if (hasX ^ hasY)
        {
            var missingSideHasNoEvents = hasX ? yEvents.Count == 0 : xEvents.Count == 0;
            if (missingSideHasNoEvents)
            {
                Warn(
                    $"Move interval [{start:F3}, {end:F3}] only has {(hasX ? "X" : "Y")} events; other side has no events, supplemented with historical constant value");
                AppendMoveEvent(target, xEvents, yEvents, start, end, 1);
                return true;
            }

            Warn(
                $"Move interval [{start:F3}, {end:F3}] only has {(hasX ? "X" : "Y")} active event while opposite side is misaligned; cut interval events via EventCutter then linearize");
            return AppendCutMoveInterval(target, xEvents, yEvents, startBeat, endBeat);
        }

        Warn(
            $"Move interval [{start:F3}, {end:F3}] X/Y events are not directly alignable; cut interval events via EventCutter then linearize");
        return AppendCutMoveInterval(target, xEvents, yEvents, startBeat, endBeat);
    }

    /// <summary>
    /// 判断 X/Y 事件是否在当前区间完全对齐（同起止）。
    /// </summary>
    private static bool IsEventExactMatch(Nrc.Event<double> xEvent, Nrc.Event<double> yEvent, float start, float end)
        => Math.Abs((double)xEvent.StartBeat - start) <= FloatEpsilon
           && Math.Abs((double)yEvent.StartBeat - start) <= FloatEpsilon
           && Math.Abs((double)xEvent.EndBeat - end) <= FloatEpsilon
           && Math.Abs((double)yEvent.EndBeat - end) <= FloatEpsilon;

    /// <summary>
    /// 向 PE 添加单段 MoveEvent。
    /// </summary>
    private static void AppendMoveEvent(
        Pe.JudgeLine target,
        List<Nrc.Event<double>> xEvents,
        List<Nrc.Event<double>> yEvents,
        float start,
        float end,
        int easing)
    {
        var endBeat = new Beat(end);
        target.MoveEvents.Add(new Pe.MoveEvent
        {
            StartBeat = start,
            EndBeat = end,
            EasingType = easing,
            EndXValue = ToPeX(GetValueAt(xEvents, endBeat)),
            EndYValue = ToPeY(GetValueAt(yEvents, endBeat))
        });
    }

    /// <summary>
    /// 对指定 Move 区间使用 EventCutter 切割，并输出线性子事件。
    /// </summary>
    private static bool AppendCutMoveInterval(
        Pe.JudgeLine target,
        List<Nrc.Event<double>> xEvents,
        List<Nrc.Event<double>> yEvents,
        Beat startBeat,
        Beat endBeat)
    {
        var interval = Math.Max((double)(endBeat - startBeat), 1e-6d);
        var cutLength = interval / UnsupportedEasingCutPrecision;
        var cutX = NrcEventTools.CutEventsInRange(xEvents, startBeat, endBeat, cutLength);
        var cutY = NrcEventTools.CutEventsInRange(yEvents, startBeat, endBeat, cutLength);

        var boundaries = CollectBoundaries(cutX, cutY);
        boundaries.Add((float)(double)startBeat);
        boundaries.Add((float)(double)endBeat);
        boundaries = boundaries.Distinct().OrderBy(v => v).ToList();

        var emitted = false;
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var segStart = boundaries[i];
            var segEnd = boundaries[i + 1];
            if (segEnd - segStart <= FloatEpsilon) continue;
            AppendMoveEvent(target, xEvents, yEvents, segStart, segEnd, 1);
            emitted = true;
        }

        return emitted;
    }

    /// <summary>
    /// 转换 double 标量事件通道（如 Rotate）。
    /// </summary>
    private static void ConvertScalarEvents(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<Nrc.Event<double>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName)
    {
        ConvertScalarEventsInternal(targetFrames, targetEvents, sourceEvents, valueTransform, channelName);
    }

    /// <summary>
    /// 标量事件转换通用实现。
    /// </summary>
    /// <typeparam name="T">NRC 标量值类型（float/double/int）。</typeparam>
    /// <param name="targetFrames">输出帧列表。</param>
    /// <param name="targetEvents">输出事件列表；传 <see langword="null"/> 时表示该通道仅导出帧。</param>
    /// <param name="sourceEvents">输入事件列表。</param>
    /// <param name="valueTransform">数值变换（坐标/范围/系数）。</param>
    /// <param name="channelName">通道名，用于告警上下文。</param>
    private static void ConvertScalarEventsInternal<T>(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<Nrc.Event<T>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName)
    {
        if (sourceEvents == null || sourceEvents.Count == 0) return;
        WarnIfNrcEventPayloadUnsupported(sourceEvents, channelName);

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
    private static int SafeConvertEasingToInt(Nrc.Easing easing, string context)
    {
        try
        {
            return (int)ConvertEasing(easing);
        }
        catch (NrcToCmdysjEasings.EasingNotSupportedException)
        {
            Warn($"{context}: unsupported easing remained after expansion, fallback to linear(1)");
            return 1;
        }
    }

    /// <summary>
    /// 对 Move 双精度事件列表进行展开：不支持缓动会切割为线性分段。
    /// </summary>
    private static List<Nrc.Event<double>> ExpandEventsForUnsupportedEasing(
        List<Nrc.Event<double>> source,
        string channel)
    {
        var expanded = new List<Nrc.Event<double>>();
        foreach (var ev in source.OrderBy(e => (double)e.StartBeat))
        {
            expanded.AddRange(ExpandUnsupportedEasing(ev, $"{channel}@{(double)ev.StartBeat:F3}"));
        }

        return expanded;
    }

    /// <summary>
    /// 若事件缓动不受支持，切割为多段线性事件拟合曲线。
    /// </summary>
    private static List<Nrc.Event<T>> ExpandUnsupportedEasing<T>(Nrc.Event<T> src, string context)
    {
        try
        {
            _ = ConvertEasing(src.Easing);
            return [src];
        }
        catch (NrcToCmdysjEasings.EasingNotSupportedException)
        {
            Warn($"{context}: unsupported easing will be sliced into {UnsupportedEasingCutPrecision} linear segments");
            return CutEventToLinear(src);
        }
    }

    /// <summary>
    /// 将单个 NRC 事件按固定精度切割为线性事件。
    /// </summary>
    private static List<Nrc.Event<T>> CutEventToLinear<T>(Nrc.Event<T> src)
    {
        var result = new List<Nrc.Event<T>>(UnsupportedEasingCutPrecision);
        var totalBeats = (double)(src.EndBeat - src.StartBeat);
        var current = src.StartBeat;
        var currentVal = src.GetValueAtBeat(src.StartBeat);

        for (var i = 0; i < UnsupportedEasingCutPrecision; i++)
        {
            var isLast = i == UnsupportedEasingCutPrecision - 1;
            var next = isLast
                ? src.EndBeat
                : new Beat((double)src.StartBeat + (i + 1.0) / UnsupportedEasingCutPrecision * totalBeats);
            var nextVal = isLast ? src.EndValue : src.GetValueAtBeat(next);

            result.Add(new Nrc.Event<T>
            {
                StartBeat = new Beat((int[])current),
                EndBeat = new Beat((int[])next),
                StartValue = currentVal,
                EndValue = nextVal,
                Easing = new Nrc.Easing(1),
                BezierPoints = new float[4],
                EasingLeft = 0f,
                EasingRight = 1f,
                Font = src.Font
            });

            current = next;
            currentVal = nextVal;
        }

        return result;
    }

    /// <summary>
    /// 收集多个事件列表的起止边界，并去重升序返回。
    /// </summary>
    private static List<float> CollectBoundaries(params List<Nrc.Event<double>>[] eventLists)
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

    private static Nrc.Event<double>? FindActiveEvent(List<Nrc.Event<double>> events, Beat beat)
    {
        var beatValue = (double)beat;
        return events.FirstOrDefault(e =>
            beatValue + FloatEpsilon >= (double)e.StartBeat
            && beatValue < (double)e.EndBeat - FloatEpsilon);
    }

    /// <summary>
    /// 获取指定拍点上的事件值。
    /// </summary>
    /// <remarks>
    /// 命中区间返回插值值；未命中时回退到最近历史事件 EndValue；再无历史时返回 0。
    /// </remarks>
    private static double GetValueAt(List<Nrc.Event<double>> events, Beat beat)
    {
        if (events.Count == 0) return 0d;
        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (beat >= e.StartBeat && beat <= e.EndBeat) return e.GetValueAtBeat(beat);
            if (beat < e.StartBeat) break;
        }

        var previous = events.LastOrDefault(e => beat > e.EndBeat);
        return previous?.EndValue ?? 0d;
    }

    private static float ToSingle<T>(T value) => value switch
    {
        float v => v,
        double v => (float)v,
        int v => v,
        _ => throw new NotSupportedException($"Unsupported scalar event value type: {typeof(T).Name}")
    };

    private static bool HasAnyEventData(Nrc.EventLayer layer)
        => (layer.MoveXEvents?.Count ?? 0) > 0
           || (layer.MoveYEvents?.Count ?? 0) > 0
           || (layer.RotateEvents?.Count ?? 0) > 0
           || (layer.AlphaEvents?.Count ?? 0) > 0
           || (layer.SpeedEvents?.Count ?? 0) > 0;

    /// <summary>
    /// 检查 Meta 中 PE 不支持的字段是否出现非默认值，并逐项告警。
    /// </summary>
    private static void WarnIfUnsupportedMeta(Nrc.Meta src)
    {
        var defaults = new Nrc.Meta();
        if (src.Background != defaults.Background)
            Warn($"Meta.Background is unsupported by PE (value='{src.Background}')");
        if (src.Author != defaults.Author) Warn($"Meta.Author is unsupported by PE (value='{src.Author}')");
        if (src.Composer != defaults.Composer) Warn($"Meta.Composer is unsupported by PE (value='{src.Composer}')");
        if (src.Artist != defaults.Artist) Warn($"Meta.Artist is unsupported by PE (value='{src.Artist}')");
        if (src.Level != defaults.Level) Warn($"Meta.Level is unsupported by PE (value='{src.Level}')");
        if (src.Name != defaults.Name) Warn($"Meta.Name is unsupported by PE (value='{src.Name}')");
        if (src.Song != defaults.Song) Warn($"Meta.Song is unsupported by PE (value='{src.Song}')");
    }

    /// <summary>
    /// 检查判定线中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private static void WarnIfUnsupportedJudgeLineFields(Nrc.JudgeLine src)
    {
        if (!string.Equals(src.Name, "NrcJudgeLine", StringComparison.Ordinal))
            Warn($"JudgeLine.Name is unsupported by PE (value='{src.Name}')");
        if (!string.Equals(src.Texture, "line.png", StringComparison.Ordinal))
            Warn($"JudgeLine.Texture is unsupported by PE (value='{src.Texture}')");
        if (!IsDefaultAnchor(src.Anchor))
            Warn($"JudgeLine.Anchor is unsupported by PE (value='[{string.Join(", ", src.Anchor)}]')");
        if (src.Father != -1)
            Warn($"JudgeLine.Father is unsupported by PE (value={src.Father}), will be auto unbind");
        if (!src.IsCover)
            Warn($"JudgeLine.IsCover is unsupported by PE (value={src.IsCover})");
        if (src.ZOrder != 0)
            Warn($"JudgeLine.ZOrder is unsupported by PE (value={src.ZOrder})");
        if (src.AttachUi.HasValue)
            Warn($"JudgeLine.AttachUi is unsupported by PE (value={(int)src.AttachUi.Value})");
        if (src.IsGif)
            Warn($"JudgeLine.IsGif is unsupported by PE (value={src.IsGif})");
        if (Math.Abs(src.BpmFactor - 1f) > FloatEpsilon)
            Warn($"JudgeLine.BpmFactor is unsupported by PE (value={src.BpmFactor})");

        if (HasNonDefaultExtendLayer(src.Extended))
            Warn("JudgeLine.Extended is unsupported by PE (contains non-default data)");
        if (!IsDefaultXControls(src.PositionControls))
            Warn("JudgeLine.PositionControls is unsupported by PE (contains non-default data)");
        if (!IsDefaultAlphaControls(src.AlphaControls))
            Warn("JudgeLine.AlphaControls is unsupported by PE (contains non-default data)");
        if (!IsDefaultSizeControls(src.SizeControls))
            Warn("JudgeLine.SizeControls is unsupported by PE (contains non-default data)");
        if (!IsDefaultSkewControls(src.SkewControls))
            Warn("JudgeLine.SkewControls is unsupported by PE (contains non-default data)");
        if (!IsDefaultYControls(src.YControls))
            Warn("JudgeLine.YControls is unsupported by PE (contains non-default data)");
    }

    /// <summary>
    /// 检查音符中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private static void WarnIfUnsupportedNoteFields(Nrc.Note src)
    {
        if (src.Alpha != 255)
            Warn($"Note.Alpha is unsupported by PE (value={src.Alpha})");
        if (Math.Abs(src.JudgeArea - 1f) > FloatEpsilon)
            Warn($"Note.JudgeArea is unsupported by PE (value={src.JudgeArea})");
        if (Math.Abs(src.VisibleTime - 999999f) > FloatEpsilon)
            Warn($"Note.VisibleTime is unsupported by PE (value={src.VisibleTime})");
        if (Math.Abs(src.YOffset) > FloatEpsilon)
            Warn($"Note.YOffset is unsupported by PE (value={src.YOffset})");
        if (!IsDefaultTint(src.Tint))
            Warn($"Note.Tint is unsupported by PE (value='[{string.Join(", ", src.Tint)}]')");
        if (src.HitFxColor != null)
            Warn($"Note.HitFxColor is unsupported by PE (value='[{string.Join(", ", src.HitFxColor)}]')");
        if (!string.IsNullOrWhiteSpace(src.HitSound))
            Warn($"Note.HitSound is unsupported by PE (value='{src.HitSound}')");
    }

    /// <summary>
    /// 检查 NRC 事件载荷中 PE 无法表达的信息，并输出告警。
    /// </summary>
    private static void WarnIfNrcEventPayloadUnsupported<T>(IEnumerable<Nrc.Event<T>> events, string channel)
    {
        foreach (var e in events)
        {
            if (e.IsBezier)
                Warn($"{channel}: Bezier event is unsupported by PE native event model");
            if (!IsDefaultBezierPoints(e.BezierPoints))
                Warn($"{channel}: non-default BezierPoints will be dropped");
            if (Math.Abs(e.EasingLeft) > FloatEpsilon || Math.Abs(e.EasingRight - 1f) > FloatEpsilon)
                Warn($"{channel}: EasingLeft/EasingRight trimming is unsupported by PE");
            if (!string.IsNullOrWhiteSpace(e.Font))
                Warn($"{channel}: Font is unsupported by PE");
        }
    }

    private static bool HasNonDefaultExtendLayer(Nrc.ExtendLayer? layer)
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

    private static bool IsDefaultXControls(List<Nrc.XControl>? controls)
        => controls == null || controls.Count == 0 || controls.SequenceEqual(Nrc.XControl.Default);

    private static bool IsDefaultAlphaControls(List<Nrc.AlphaControl>? controls)
        => controls == null || controls.Count == 0 || controls.SequenceEqual(Nrc.AlphaControl.Default);

    private static bool IsDefaultSizeControls(List<Nrc.SizeControl>? controls)
        => controls == null || controls.Count == 0 || controls.SequenceEqual(Nrc.SizeControl.Default);

    private static bool IsDefaultSkewControls(List<Nrc.SkewControl>? controls)
        => controls == null || controls.Count == 0 || controls.SequenceEqual(Nrc.SkewControl.Default);

    private static bool IsDefaultYControls(List<Nrc.YControl>? controls)
        => controls == null || controls.Count == 0 || controls.SequenceEqual(Nrc.YControl.Default);

    /// <summary>
    /// 输出 NrcToPe 统一前缀的告警日志。
    /// </summary>
    private static void Warn(string message) => NrcToolLog.OnWarning($"[NrcToPe] {message}");
}