using System.Diagnostics.Contracts;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events;

namespace PhiFanmade.Tool.PhiEdit.Converters;

/// <summary>
/// PhiEdit 格式 → NRC 格式转换器。
/// </summary>
public static class PeToNrc
{
    /// <summary>
    /// 在末尾额外补齐的拍点长度，避免最后一个关键帧/事件在 NRC 中没有后续区间可承载。
    /// </summary>
    private const double TrailingBeatPadding = 1d / 64d;

    /// <summary>
    /// 非事件起点 Frame 在 NRC 中保留的最小可编辑区间长度（拍）。
    /// </summary>
    private const double FrameEditableSliceBeat = 0.0125d;

    /// <summary>
    /// 拍点比较容差。
    /// </summary>
    private const double BeatComparisonEpsilon = 1e-6d;

    /// <summary>
    /// PhiEdit 坐标系配置，用于将 PE 坐标统一映射到 NRC 归一化坐标系。
    /// </summary>
    private static readonly CoordinateProfile PeCoordinateProfile = new(
        Pe.Chart.CoordinateSystem.MinX,
        Pe.Chart.CoordinateSystem.MaxX,
        Pe.Chart.CoordinateSystem.MinY,
        Pe.Chart.CoordinateSystem.MaxY,
        Pe.Chart.CoordinateSystem.ClockwiseRotation);

    /// <summary>
    /// 偏移的偏移常数。
    /// </summary>
    private const int OffsetOffset = 175;

    /// <summary>
    /// 将 PhiEdit 谱面转换为 NRC 谱面。
    /// </summary>
    /// <param name="pe">待转换的 PhiEdit 谱面。</param>
    /// <returns>转换后的 NRC 谱面对象。</returns>
    [Pure]
    public static Nrc.Chart Convert(Pe.Chart pe)
    {
        ArgumentNullException.ThrowIfNull(pe);

        return new Nrc.Chart
        {
            BpmList = pe.BpmList?.ConvertAll(ConvertBpmItem) ?? [],
            Meta = ConvertMeta(pe),
            JudgeLineList = ConvertJudgeLines(pe.JudgeLineList)
        };
    }

    /// <summary>
    /// 将 PhiEdit 缓动编号映射为 NRC 缓动编号。
    /// </summary>
    /// <param name="pe">PhiEdit 缓动编号。</param>
    /// <returns>对应的 NRC 缓动编号；未知编号时回退为线性。</returns>
    [Pure]
    private static int MapEasingNumber(int pe) => pe switch
    {
        1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
        8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
        14 => 15, 15 => 14, 16 => 18, 17 => 17, 18 => 21, 19 => 20,
        20 => 24, 21 => 23, 22 => 22, 23 => 25, 24 => 27, 25 => 26,
        26 => 30, 27 => 29, 28 => 31, 29 => 28, _ => 1
    };

    /// <summary>将 PhiEdit 缓动对象转换为 NRC 缓动对象。</summary>
    private static Nrc.Easing ConvertEasing(Pe.Easing src) => new(MapEasingNumber((int)src));

    /// <summary>将 PhiEdit X 坐标转换为 NRC X 坐标。</summary>
    private static double TransformX(float x) => CoordinateGeometry.ToNrcX(x, PeCoordinateProfile);

    /// <summary>将 PhiEdit Y 坐标转换为 NRC Y 坐标。</summary>
    private static double TransformY(float y) => CoordinateGeometry.ToNrcY(y, PeCoordinateProfile);

    /// <summary>将 PhiEdit 角度转换为 NRC 角度方向。</summary>
    private static double TransformAngle(float angle) => CoordinateGeometry.ToNrcAngle(angle, PeCoordinateProfile);

    /// <summary>
    /// 转换单个 BPM 点。
    /// </summary>
    /// <param name="src">PhiEdit BPM 点。</param>
    /// <returns>NRC BPM 点。</returns>
    private static Nrc.BpmItem ConvertBpmItem(Pe.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = new Beat(src.StartBeat)
    };

    /// <summary>
    /// 生成 NRC 元数据。PhiEdit 缺失的大部分元信息保持 NRC 默认值，仅覆盖可直接映射项。
    /// </summary>
    /// <param name="src">PhiEdit 谱面。</param>
    /// <returns>NRC 元数据。</returns>
    private static Nrc.Meta ConvertMeta(Pe.Chart src) => new()
    {
        Offset = src.Offset - OffsetOffset // WTF
    };

    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    /// <param name="judgeLines">PhiEdit 判定线列表。</param>
    /// <returns>转换后的 NRC 判定线列表；输入为空时返回空列表。</returns>
    private static List<Nrc.JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines)
    {
        if (judgeLines == null || judgeLines.Count == 0) return [];

        var result = new List<Nrc.JudgeLine>(judgeLines.Count);
        result.AddRange(judgeLines.Select(ConvertJudgeLine));
        return result;
    }

    /// <summary>
    /// 转换单条判定线，并合成为单事件层的 NRC 判定线。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <param name="index">判定线索引，用于生成默认名称。</param>
    /// <returns>转换后的 NRC 判定线。</returns>
    private static Nrc.JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index)
    {
        var horizonBeat = GetJudgeLineHorizonBeat(src);
        var eventLayer = ConvertEventLayer(src, horizonBeat);
        eventLayer.Anticipation();

        return new Nrc.JudgeLine
        {
            Name = $"PeJudgeLine_{index}",
            Notes = src.NoteList?.ConvertAll(ConvertNote) ?? [],
            EventLayers = [eventLayer]
        };
    }

    /// <summary>
    /// 转换单个音符。
    /// </summary>
    /// <param name="src">PhiEdit 音符。</param>
    /// <returns>NRC 音符。</returns>
    private static Nrc.Note ConvertNote(Pe.Note src) => new()
    {
        Above = src.Above,
        StartBeat = new Beat(src.StartBeat),
        EndBeat = new Beat(src.EndBeat),
        IsFake = src.IsFake,
        PositionX = TransformX(src.PositionX) + Nrc.Chart.CoordinateSystem.MaxX,
        Size = src.WidthRatio,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Nrc.NoteType)(int)src.Type
    };

    /// <summary>
    /// 将 PhiEdit 判定线上的各通道帧/事件规范化为 NRC 事件层。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <param name="horizonBeat">用于补尾区间的终止拍点。</param>
    /// <returns>构建后的 NRC 事件层。</returns>
    private static Nrc.EventLayer ConvertEventLayer(Pe.JudgeLine src, double horizonBeat) => new()
    {
        MoveXEvents = BuildMoveAxisEvents(src.MoveFrames, src.MoveEvents, horizonBeat, point => point.X, TransformX),
        MoveYEvents = BuildMoveAxisEvents(src.MoveFrames, src.MoveEvents, horizonBeat, point => point.Y, TransformY),
        RotateEvents = BuildScalarEvents(src.RotateFrames, src.RotateEvents, horizonBeat, TransformAngle),
        AlphaEvents = BuildScalarEvents(src.AlphaFrames, src.AlphaEvents, horizonBeat,
            value => Math.Clamp((int)Math.Round(value), 0, 255)),
        SpeedEvents = BuildScalarEvents(src.SpeedFrames, [], horizonBeat, value => (float)(value / (14d / 9d)))
    };

    /// <summary>
    /// 计算单条判定线的时间范围上界，并额外补齐一个微小区间。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <returns>用于事件构建的尾部拍点。</returns>
    private static double GetJudgeLineHorizonBeat(Pe.JudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.EndBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.StartBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.SpeedFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.MoveFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.RotateFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.AlphaFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.MoveEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.RotateEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.AlphaEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        return maxBeat + TrailingBeatPadding;
    }

    /// <summary>
    /// 获取拍点集合中的最大值；为空时返回 <c>0</c>。
    /// </summary>
    /// <param name="beats">拍点集合。</param>
    /// <returns>最大拍点。</returns>
    private static double GetMaxBeat(IEnumerable<double>? beats)
    {
        return beats?.DefaultIfEmpty(0d).Max() ?? 0d;
    }

    /// <summary>
    /// 构建 Move 轴（X 或 Y）事件列表。
    /// </summary>
    /// <param name="frames">Move 帧列表。</param>
    /// <param name="events">Move 事件列表。</param>
    /// <param name="horizonBeat">用于补尾区间的终止拍点。</param>
    /// <param name="selector">从二维坐标中提取目标轴值的选择器。</param>
    /// <param name="valueTransformer">坐标值转换器（例如坐标系归一化）。</param>
    /// <returns>构建后的 NRC 事件列表；无有效区间时返回 <see langword="null"/>。</returns>
    private static List<Nrc.Event<double>>? BuildMoveAxisEvents(
        List<Pe.MoveFrame>? frames,
        List<Pe.MoveEvent>? events,
        double horizonBeat,
        Func<(float X, float Y), float> selector,
        Func<float, double> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Nrc.Event<double>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindMoveFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsMoveEventStartBeat(orderedEvents, startBeat))
            {
                var convertedValue = valueTransformer(selector((frameAtBoundary.XValue, frameAtBoundary.YValue)));
                result.Add(CreateConstantEvent(startBeat, endBeat, convertedValue));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveMoveEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveMoveEventStartValue(activeEvent, orderedFrames, orderedEvents);
                result.Add(new Nrc.Event<double>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue =
                        valueTransformer(InterpolateMoveValue(activeEvent, startBeat, eventStartSource, selector)),
                    EndValue = valueTransformer(InterpolateMoveValue(activeEvent, endBeat, eventStartSource,
                        selector))
                });
            }
            else
            {
                // NRC 在无事件区间会沿用上一个事件结束值，这里不再填充冗余常值事件。
            }
        }

        return result.Count == 0 ? null : NrcEventTools.EventListCompress(result, 0d);
    }

    /// <summary>
    /// 构建标量通道（旋转、透明度、速度）事件列表。
    /// </summary>
    /// <typeparam name="T">目标 NRC 通道数值类型。</typeparam>
    /// <param name="frames">标量帧列表。</param>
    /// <param name="events">标量事件列表。</param>
    /// <param name="horizonBeat">用于补尾区间的终止拍点。</param>
    /// <param name="valueTransformer">标量值转换器。</param>
    /// <returns>构建后的 NRC 事件列表；无有效区间时返回 <see langword="null"/>。</returns>
    private static List<Nrc.Event<T>>? BuildScalarEvents<T>(
        List<Pe.Frame>? frames,
        List<Pe.Event>? events,
        double horizonBeat,
        Func<float, T> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Nrc.Event<T>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindScalarFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsScalarEventStartBeat(orderedEvents, startBeat))
            {
                result.Add(CreateConstantEvent(startBeat, endBeat, valueTransformer(frameAtBoundary.Value)));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveScalarEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveScalarEventStartValue(activeEvent, orderedFrames, orderedEvents);
                result.Add(new Nrc.Event<T>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue = valueTransformer(InterpolateScalarValue(activeEvent, startBeat, eventStartSource)),
                    EndValue = valueTransformer(InterpolateScalarValue(activeEvent, endBeat, eventStartSource))
                });
            }
            else
            {
                // NRC 在无事件区间会沿用上一个事件结束值，这里不再填充冗余常值事件。
            }
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// 合并帧边界与事件边界，并补齐尾部边界。
    /// </summary>
    /// <param name="frameBoundaries">帧边界拍点。</param>
    /// <param name="eventBoundaries">事件起止边界拍点。</param>
    /// <param name="horizonBeat">用于补尾区间的候选终止拍点。</param>
    /// <returns>升序去重后的边界列表；无输入边界时返回空列表。</returns>
    private static List<double> BuildBoundaries(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        double horizonBeat)
    {
        var boundaries = new SortedSet<double>();
        foreach (var beat in frameBoundaries) boundaries.Add(beat);
        foreach (var beat in eventBoundaries) boundaries.Add(beat);
        if (boundaries.Count == 0) return [];

        boundaries.Add(Math.Max(horizonBeat, boundaries.Max + TrailingBeatPadding));
        return boundaries.ToList();
    }

    /// <summary>
    /// 在基础边界上为非事件起点的 Frame 追加短切片边界，用于保留可编辑性。
    /// </summary>
    private static List<double> BuildBoundariesWithFrameSlices(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        IEnumerable<double> eventStartBoundaries,
        double horizonBeat)
    {
        var frameList = frameBoundaries.ToList();
        var eventStartList = eventStartBoundaries.ToList();
        var boundaries = BuildBoundaries(frameList, eventBoundaries, horizonBeat);
        if (boundaries.Count == 0) return boundaries;

        var expandedBoundaries = new SortedSet<double>(boundaries);
        foreach (var frameBeat in frameList)
        {
            if (eventStartList.Any(startBeat => IsSameBeat(startBeat, frameBeat))) continue;
            expandedBoundaries.Add(frameBeat + FrameEditableSliceBeat);
        }

        return expandedBoundaries.ToList();
    }

    /// <summary>
    /// 获取区间中点拍。
    /// </summary>
    /// <param name="startBeat">区间起点拍。</param>
    /// <param name="endBeat">区间终点拍。</param>
    /// <returns>中点拍。</returns>
    private static double GetMidBeat(double startBeat, double endBeat) => startBeat + (endBeat - startBeat) / 2d;

    /// <summary>
    /// 判断两个拍点是否近似相等。
    /// </summary>
    private static bool IsSameBeat(double leftBeat, double rightBeat)
        => Math.Abs(leftBeat - rightBeat) <= BeatComparisonEpsilon;

    /// <summary>
    /// 将绝对拍点映射为事件内部归一化边界（供 NRC 的 EasingLeft/EasingRight 使用）。
    /// </summary>
    /// <param name="eventStartBeat">事件起点拍。</param>
    /// <param name="eventEndBeat">事件终点拍。</param>
    /// <param name="beat">待映射拍点。</param>
    /// <returns>归一化边界值；零时长事件返回 <c>1</c>。</returns>
    private static float GetEventBoundary(float eventStartBeat, float eventEndBeat, double beat)
    {
        var duration = eventEndBeat - eventStartBeat;
        if (Math.Abs(duration) < 1e-6f) return 1f;
        return (float)((beat - eventStartBeat) / duration);
    }

    /// <summary>
    /// 创建常值线性事件（起止值相同）。
    /// </summary>
    /// <typeparam name="T">事件值类型。</typeparam>
    /// <param name="startBeat">事件起点拍。</param>
    /// <param name="endBeat">事件终点拍。</param>
    /// <param name="value">常值。</param>
    /// <returns>常值 NRC 事件。</returns>
    private static Nrc.Event<T> CreateConstantEvent<T>(double startBeat, double endBeat, T value) => new()
    {
        StartBeat = new Beat(startBeat),
        EndBeat = new Beat(endBeat),
        Easing = new Nrc.Easing(1),
        EasingLeft = 0f,
        EasingRight = 1f,
        StartValue = value,
        EndValue = value
    };

    /// <summary>
    /// 查找指定拍点正在生效的 Move 事件。
    /// </summary>
    /// <param name="events">已按起点排序的 Move 事件列表。</param>
    /// <param name="beat">目标拍点。</param>
    /// <returns>命中的事件；若不存在返回 <see langword="null"/>。</returns>
    private static Pe.MoveEvent? FindActiveMoveEvent(List<Pe.MoveEvent> events, double beat)
    {
        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];
            if (beat >= ev.StartBeat && beat <= ev.EndBeat)
                return ev;
            if (beat < ev.StartBeat)
                break;
        }

        return null;
    }

    /// <summary>
    /// 查找指定拍点正在生效的标量事件。
    /// </summary>
    /// <param name="events">已按起点排序的标量事件列表。</param>
    /// <param name="beat">目标拍点。</param>
    /// <returns>命中的事件；若不存在返回 <see langword="null"/>。</returns>
    private static Pe.Event? FindActiveScalarEvent(List<Pe.Event> events, double beat)
    {
        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];
            if (beat >= ev.StartBeat && beat <= ev.EndBeat)
                return ev;
            if (beat < ev.StartBeat)
                break;
        }

        return null;
    }

    /// <summary>
    /// 解析某边界拍之后 Move 通道的生效值。
    /// 优先级：最近结束事件 > 最近前置帧 > 默认值 (0,0)。
    /// </summary>
    /// <param name="frames">Move 帧列表。</param>
    /// <param name="events">Move 事件列表。</param>
    /// <param name="boundaryBeat">边界拍点。</param>
    /// <returns>边界拍后的生效坐标。</returns>
    private static (float X, float Y) ResolveMoveValueAfterBoundary(
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events,
        double boundaryBeat)
    {
        var previousFrame = frames.LastOrDefault(frame => frame.Beat <= boundaryBeat);
        var previousEvent = events.LastOrDefault(ev => ev.EndBeat <= boundaryBeat);

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return (previousEvent.EndXValue, previousEvent.EndYValue);

        if (previousFrame != null)
            return (previousFrame.XValue, previousFrame.YValue);

        return (0f, 0f);
    }

    /// <summary>
    /// 解析某边界拍之后标量通道的生效值。
    /// 优先级：最近结束事件 > 最近前置帧 > 默认值 0。
    /// </summary>
    /// <param name="frames">标量帧列表。</param>
    /// <param name="events">标量事件列表。</param>
    /// <param name="boundaryBeat">边界拍点。</param>
    /// <returns>边界拍后的生效值。</returns>
    private static float ResolveScalarValueAfterBoundary(
        List<Pe.Frame> frames,
        List<Pe.Event> events,
        double boundaryBeat)
    {
        var previousFrame = frames.LastOrDefault(frame => frame.Beat <= boundaryBeat);
        var previousEvent = events.LastOrDefault(ev => ev.EndBeat <= boundaryBeat);

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return previousEvent.EndValue;

        if (previousFrame != null)
            return previousFrame.Value;

        return 0f;
    }

    /// <summary>
    /// 解析 Move 事件的起始源值；若事件起点存在 Frame，则优先使用该 Frame。
    /// </summary>
    private static (float X, float Y) ResolveMoveEventStartValue(
        Pe.MoveEvent ev,
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events)
    {
        var frameAtStart = FindMoveFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return (frameAtStart.XValue, frameAtStart.YValue);

        return ResolveMoveValueAfterBoundary(frames, events, ev.StartBeat);
    }

    /// <summary>
    /// 解析标量事件的起始源值；若事件起点存在 Frame，则优先使用该 Frame。
    /// </summary>
    private static float ResolveScalarEventStartValue(Pe.Event ev, List<Pe.Frame> frames, List<Pe.Event> events)
    {
        var frameAtStart = FindScalarFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return frameAtStart.Value;

        return ResolveScalarValueAfterBoundary(frames, events, ev.StartBeat);
    }

    /// <summary>
    /// 判断拍点是否正好位于某个 Move 事件起点。
    /// </summary>
    private static bool IsMoveEventStartBeat(List<Pe.MoveEvent> events, double beat)
        => events.Any(ev => IsSameBeat(ev.StartBeat, beat));

    /// <summary>
    /// 判断拍点是否正好位于某个标量事件起点。
    /// </summary>
    private static bool IsScalarEventStartBeat(List<Pe.Event> events, double beat)
        => events.Any(ev => IsSameBeat(ev.StartBeat, beat));

    /// <summary>
    /// 获取指定拍点上的 Move Frame。
    /// </summary>
    private static Pe.MoveFrame? FindMoveFrameAtBeat(List<Pe.MoveFrame> frames, double beat)
        => frames.LastOrDefault(frame => IsSameBeat(frame.Beat, beat));

    /// <summary>
    /// 获取指定拍点上的标量 Frame。
    /// </summary>
    private static Pe.Frame? FindScalarFrameAtBeat(List<Pe.Frame> frames, double beat)
        => frames.LastOrDefault(frame => IsSameBeat(frame.Beat, beat));

    /// <summary>
    /// 在指定拍点对 Move 事件进行插值。
    /// </summary>
    /// <param name="ev">Move 事件。</param>
    /// <param name="beat">采样拍点。</param>
    /// <param name="intervalStartSource">当前区间的起始源值。</param>
    /// <param name="selector">轴选择器（X 或 Y）。</param>
    /// <returns>插值结果。</returns>
    private static float InterpolateMoveValue(
        Pe.MoveEvent ev,
        double beat,
        (float X, float Y) intervalStartSource,
        Func<(float X, float Y), float> selector)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return selector((ev.EndXValue, ev.EndYValue));

        return selector(ev.GetValueAtBeat((float)beat, intervalStartSource.X, intervalStartSource.Y));
    }

    /// <summary>
    /// 在指定拍点对标量事件进行插值。
    /// </summary>
    /// <param name="ev">标量事件。</param>
    /// <param name="beat">采样拍点。</param>
    /// <param name="intervalStartSource">当前区间的起始源值。</param>
    /// <returns>插值结果。</returns>
    private static float InterpolateScalarValue(Pe.Event ev, double beat, float intervalStartSource)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return ev.EndValue;

        return ev.GetValueAtBeat((float)beat, intervalStartSource);
    }
}