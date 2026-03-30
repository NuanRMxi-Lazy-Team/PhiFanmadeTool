using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.RePhiEdit.Events.Internal;

namespace PhiFanmade.Tool.RePhiEdit.JudgeLines.Internal;

/// <summary>
/// RPE 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、结果写回。
/// </summary>
internal static class FatherUnbindHelpers
{
    internal static readonly ConditionalWeakTable<List<Rpe.JudgeLine>, ConcurrentDictionary<int, Rpe.JudgeLine>>
        ChartCacheTable = new();

    internal static (double, double) GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        double rad = (angleDegrees % 360) * Math.PI / 180d;
        double rotX = lineX * Math.Cos(rad) + lineY * Math.Sin(rad);
        double rotY = -lineX * Math.Sin(rad) + lineY * Math.Cos(rad);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    internal static float GetValIn(List<Rpe.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat) { idx = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    internal static float GetValOut(List<Rpe.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat < beat) { idx = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat >= beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    internal static List<Rpe.Event<float>> MergeLayerChannel(
        List<Rpe.EventLayer> layers,
        Func<Rpe.EventLayer, List<Rpe.Event<float>>?> selector,
        Func<List<Rpe.Event<float>>, List<Rpe.Event<float>>, List<Rpe.Event<float>>> merge)
    {
        var result = new List<Rpe.Event<float>>();
        return layers.Select(selector)
            .Where(ch => ch is { Count: > 0 })
            .Aggregate(result, (current, ch) => merge(current, ch ?? []));
    }

    internal static (Beat Min, Beat Max) GetEventRange(List<Rpe.Event<float>> events)
        => events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat), events.Max(e => e.EndBeat));

    internal static void WriteResultToLine(
        Rpe.JudgeLine line,
        List<Rpe.Event<float>> newXEvents,
        List<Rpe.Event<float>> newYEvents,
        List<Rpe.Event<float>> fatherRotateEvents,
        double tolerance,
        Func<List<Rpe.Event<float>>, List<Rpe.Event<float>>, List<Rpe.Event<float>>> merge,
        bool compress = true)
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents.Clear();
            line.EventLayers[i].MoveYEvents.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new Rpe.EventLayer());

        line.EventLayers[0].MoveXEvents = compress
            ? EventCompressor.EventListCompress<float>(newXEvents, tolerance)
            : newXEvents;
        line.EventLayers[0].MoveYEvents = compress
            ? EventCompressor.EventListCompress<float>(newYEvents, tolerance)
            : newYEvents;

        if (line.RotateWithFather)
        {
            var merged = merge(line.EventLayers[0].RotateEvents, fatherRotateEvents);
            line.EventLayers[0].RotateEvents = compress
                ? EventCompressor.EventListCompress<float>(merged, tolerance)
                : merged;
        }

        line.Father = -1;
    }
}


