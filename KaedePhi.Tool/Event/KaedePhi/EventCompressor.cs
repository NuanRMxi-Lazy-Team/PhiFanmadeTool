namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// NRC 事件压缩器：合并变化率相近的相邻线性事件，以及移除无意义的默认值事件。
/// </summary>
public class EventCompressor<TPayload> : IEventCompressor<Kpc.Event<TPayload>>
{
    private static void ValidateParams(double tolerance)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (typeof(TPayload) != typeof(int) && typeof(TPayload) != typeof(float) && typeof(TPayload) != typeof(double))
            throw new NotSupportedException("EventListCompress only supports int, float, and double types.");
    }

    /// <summary>
    /// 判断两段线性事件能否合并（归一化垂直距离算法）。
    /// </summary>
    private static bool TryMergeSqrt(Kpc.Event<TPayload> last, Kpc.Event<TPayload> cur, double relTol)
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = Convert.ToDouble(last.StartValue);
        var midValueEnd = Convert.ToDouble(last.EndValue);
        var midValueStart = Convert.ToDouble(cur.StartValue);
        var endValue = Convert.ToDouble(cur.EndValue);

        var scale = Math.Max(Math.Max(Math.Abs(startValue), Math.Abs(midValueEnd)),
            Math.Max(Math.Abs(endValue), 1e-3));

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12) return true;

        var normalizedMidBeat = (midBeat - startBeat) / totalBeatSpan;
        var normalizedValueDelta = (endValue - startValue) / scale;
        var normalizedMidValue = (midValueEnd - startValue) / scale;
        var linearDeviation = normalizedMidValue - normalizedValueDelta * normalizedMidBeat;
        var mergedLineLength = Math.Sqrt(1.0 + normalizedValueDelta * normalizedValueDelta);
        return Math.Abs(linearDeviation) / mergedLineLength <= relTol;
    }

    /// <summary>
    /// 判断两段线性事件能否合并（归一化斜率差算法）。
    /// </summary>
    private static bool TryMergeSlope(Kpc.Event<TPayload> last, Kpc.Event<TPayload> cur, double relTol)
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = Convert.ToDouble(last.StartValue);
        var midValueEnd = Convert.ToDouble(last.EndValue);
        var midValueStart = Convert.ToDouble(cur.StartValue);
        var endValue = Convert.ToDouble(cur.EndValue);

        var scale = Math.Max(Math.Max(Math.Abs(startValue), Math.Abs(midValueEnd)),
            Math.Max(Math.Abs(endValue), 1e-3));

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12) return true;

        var firstSegmentDuration = midBeat - startBeat;
        var secondSegmentDuration = endBeat - midBeat;
        var firstSlope = firstSegmentDuration < 1e-12 ? 0.0 : (midValueEnd - startValue) / firstSegmentDuration / scale;
        var secondSlope = secondSegmentDuration < 1e-12
            ? 0.0
            : (endValue - midValueStart) / secondSegmentDuration / scale;
        return Math.Abs(firstSlope - secondSlope) <= relTol;
    }

    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventListCompressSqrt(
        List<Kpc.Event<TPayload>>? events, double tolerance)
    {
        ValidateParams(tolerance);
        if (events == null || events.Count == 0) return [];

        var compressed = new List<Kpc.Event<TPayload>> { events[0] };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1 &&
                lastEvent.EndBeat == currentEvent.StartBeat &&
                TryMergeSqrt(lastEvent, currentEvent, relTol))
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <inheritdoc/>
    public List<Kpc.Event<TPayload>> EventListCompressSlope(
        List<Kpc.Event<TPayload>>? events, double tolerance)
    {
        ValidateParams(tolerance);
        if (events == null || events.Count == 0) return [];

        var compressed = new List<Kpc.Event<TPayload>> { events[0] };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1 &&
                lastEvent.EndBeat == currentEvent.StartBeat &&
                TryMergeSlope(lastEvent, currentEvent, relTol))
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }
}