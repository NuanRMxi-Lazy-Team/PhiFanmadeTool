using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、结果写回。
/// 同步处理器（<see cref="FatherUnbindProcessor"/>）与异步处理器（<see cref="FatherUnbindAsyncProcessor"/>）共享此类。
/// </summary>
internal static class FatherUnbindHelpers
{
    private static readonly AsyncLocal<CoordinateProfile?> RenderProfileContext = new();

    internal static CoordinateProfile CurrentRenderProfile
        => RenderProfileContext.Value ?? CoordinateProfile.DefaultRenderProfile;

    internal static IDisposable UseRenderProfile(CoordinateProfile renderProfile)
        => new RenderProfileScope(renderProfile);

    private sealed class RenderProfileScope : IDisposable
    {
        private readonly CoordinateProfile? _previous;

        public RenderProfileScope(CoordinateProfile nextProfile)
        {
            _previous = RenderProfileContext.Value;
            RenderProfileContext.Value = nextProfile;
        }

        public void Dispose() => RenderProfileContext.Value = _previous;
    }

    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存：
    /// 同一谱面的所有解绑调用共享同一份缓存，allJudgeLines 被 GC 后自动释放。
    /// </summary>
    internal static readonly ConditionalWeakTable<List<Nrc.JudgeLine>, ConcurrentDictionary<int, Nrc.JudgeLine>>
        ChartCacheTable = new();

    /// <summary>
    /// 根据父线绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    internal static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => CoordinateGeometry.GetNrcAbsolutePos(
            fatherLineX, fatherLineY, angleDegrees, lineX, lineY, CurrentRenderProfile);

    /// <summary>
    /// NRC 虽然以归一化坐标存储，但几何误差必须在当前渲染坐标系评估，
    /// 否则 X/Y 轴缩放不一致会导致切段阈值偏斜。
    /// </summary>
    internal static bool NeedsAdaptiveCut(
        (double X, double Y) segmentStart,
        (double X, double Y) next,
        (double X, double Y) intervalEnd,
        Beat segmentStartBeat,
        Beat intervalEndBeat,
        Beat nextBeat,
        double tolerance)
    {
        var segmentLength = (double)(intervalEndBeat - segmentStartBeat);
        var progress = segmentLength > 1e-12
            ? (double)(nextBeat - segmentStartBeat) / segmentLength
            : 1.0;
        var predicted = (
            X: segmentStart.X + (intervalEnd.X - segmentStart.X) * progress,
            Y: segmentStart.Y + (intervalEnd.Y - segmentStart.Y) * progress);
        var error = CoordinateGeometry.GetNrcScreenDistance(next, predicted, CurrentRenderProfile);
        var threshold = tolerance / 100.0 *
                        ((CoordinateGeometry.GetNrcScreenMagnitude(segmentStart, CurrentRenderProfile) +
                          CoordinateGeometry.GetNrcScreenMagnitude(next, CurrentRenderProfile)) / 2.0 + 1e-9);
        return error > threshold;
    }

    /// <summary>
    /// 传入语义：取 beat 时刻正在生效的事件插值（用于段起点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValIn(List<Nrc.Event<double>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 传出语义：取 beat 时刻即将结束的事件插值（用于段终点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValOut(List<Nrc.Event<double>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat < beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat >= beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 按层顺序将某一通道的事件列表串行叠加合并。层间叠加不满足交换律，必须顺序处理。
    /// </summary>
    internal static List<Nrc.Event<double>> MergeLayerChannel(
        List<Nrc.EventLayer> layers,
        Func<Nrc.EventLayer, List<Nrc.Event<double>>?> selector,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge)
    {
        var result = new List<Nrc.Event<double>>();
        return layers.Select(selector)
            .Where(ch => ch is { Count: > 0 })
            .Aggregate(result, (current, ch) => merge(current, ch!));
    }

    /// <summary>获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。列表为空时返回 (0, 0)。</summary>
    internal static (Beat Min, Beat Max) GetEventRange(List<Nrc.Event<double>> events)
        => events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat) ?? new Beat(0), events.Max(e => e.EndBeat) ?? new Beat(0));

    /// <summary>
    /// 将计算结果写回判定线：清除第 1 层及以上的 X/Y 事件，将压缩后的结果写入第 0 层。
    /// RotateWithFather 为 true 时叠加父线旋转事件；最后置 Father = -1 完成解绑。
    /// </summary>
    internal static void WriteResultToLine(
        Nrc.JudgeLine line,
        List<Nrc.Event<double>> newXEvents,
        List<Nrc.Event<double>> newYEvents,
        List<Nrc.Event<double>> fatherRotateEvents,
        double tolerance,
        Func<List<Nrc.Event<double>>, List<Nrc.Event<double>>, List<Nrc.Event<double>>> merge,
        bool compress = true)
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents.Clear();
            line.EventLayers[i].MoveYEvents.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new Nrc.EventLayer());

        line.EventLayers[0].MoveXEvents = compress
            ? EventCompressor.EventListCompress(newXEvents, tolerance)
            : newXEvents;
        line.EventLayers[0].MoveYEvents = compress
            ? EventCompressor.EventListCompress(newYEvents, tolerance)
            : newYEvents;

        if (line.RotateWithFather)
        {
            var merged = merge(line.EventLayers[0].RotateEvents, fatherRotateEvents);
            line.EventLayers[0].RotateEvents = compress
                ? EventCompressor.EventListCompress(merged, tolerance)
                : merged;
        }

        line.Father = -1;
    }
}