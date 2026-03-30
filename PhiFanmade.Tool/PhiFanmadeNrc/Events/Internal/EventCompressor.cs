namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件压缩器：合并变化率相近的相邻线性事件，以及移除无意义的默认值事件。
/// </summary>
internal static class EventCompressor
{
    /// <summary>
    /// 压缩事件列表，合并数值变化率相同且相连的线性事件。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">拟合容差百分比，越大拟合精细度越低</param>
    internal static List<Nrc.Event<T>> EventListCompress<T>(
        List<Nrc.Event<T>>? events, double tolerance = 5)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListCompress only supports int, float, and double types.");

        if (events == null || events.Count == 0) return [];

        var compressed = new List<Nrc.Event<T>> { events[0] };

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1)
            {
                var lastRate = ((dynamic?)lastEvent.EndValue - (dynamic?)lastEvent.StartValue) /
                               (lastEvent.EndBeat - lastEvent.StartBeat);
                var currentRate = ((dynamic?)currentEvent.EndValue - (dynamic?)currentEvent.StartValue) /
                                  (currentEvent.EndBeat - currentEvent.StartBeat);

                if (Math.Abs((double)(lastRate - currentRate)) <=
                    tolerance * (Math.Abs((double)lastRate) + Math.Abs((double)currentRate)) / 2.0 / 100.0 &&
                    lastEvent.EndBeat == currentEvent.StartBeat &&
                    Math.Abs((double)((dynamic?)lastEvent.EndValue - (dynamic?)currentEvent.StartValue)) <=
                    tolerance * (Math.Abs((dynamic?)lastEvent.EndValue) + 1e-9) / 100.0)
                {
                    lastEvent.EndBeat = currentEvent.EndBeat;
                    lastEvent.EndValue = currentEvent.EndValue;
                    continue;
                }
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 移除无用事件（起始值和结束值都为默认值的事件）。
    /// </summary>
    internal static List<Nrc.Event<T>>? RemoveUselessEvent<T>(List<Nrc.Event<T>>? events)
    {
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy is { Count: 1 } &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue, default))
        {
            eventsCopy.RemoveAt(0);
        }

        return eventsCopy;
    }
}