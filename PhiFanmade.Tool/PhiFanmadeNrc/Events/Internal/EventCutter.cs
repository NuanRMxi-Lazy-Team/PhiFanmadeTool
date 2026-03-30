using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件切割器：将事件列表按指定拍长切割为等长段。
/// </summary>
internal static class EventCutter
{
    /// <summary>
    /// 在指定的拍范围内切割事件列表。
    /// </summary>
    internal static List<Nrc.Event<T>> CutEventsInRange<T>(
        List<Nrc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength)
    {
        var cutEvents = new List<Nrc.Event<T>>();
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd   = evt.EndBeat   > endBeat   ? endBeat   : evt.EndBeat;

            var totalBeats    = cutEnd - cutStart;
            var segmentCount  = (int)Math.Ceiling((totalBeats / cutLength));

            for (var i = 0; i < segmentCount; i++)
            {
                var currentBeat = new Beat(cutStart + (cutLength * i));
                var segmentEnd  = new Beat(cutStart + (cutLength * (i + 1)));
                if (segmentEnd > cutEnd) segmentEnd = cutEnd;

                cutEvents.Add(new Nrc.Event<T>
                {
                    StartBeat  = currentBeat,
                    EndBeat    = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue   = evt.GetValueAtBeat(segmentEnd),
                });
            }
        }

        return cutEvents;
    }
     
    /// <see cref="CutEventsInRange{T}(List{Nrc.Event{T}}, Beat, Beat, Beat)"/>
    internal static List<Nrc.Event<T>> CutEventsInRange<T>(
        List<Nrc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
    {
        var cutLengthBeat = new Beat(cutLength);
        return CutEventsInRange(events, startBeat, endBeat, cutLengthBeat);
    }
}

