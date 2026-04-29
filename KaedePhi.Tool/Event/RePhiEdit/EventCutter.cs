using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Event.RePhiEdit;

/// <summary>
/// NRC 事件切割器：将事件列表按指定拍长切割为等长段。
/// </summary>
public class EventCutter<TPayload> : IEventCutter<Rpe.Event<TPayload>, Beat>
{
    /// <inheritdoc/>
    public List<Rpe.Event<TPayload>> CutEventsInRange(
        List<Rpe.Event<TPayload>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
    {
        var cutLengthBeat = new Beat(cutLength);
        return CutEventsInRange(events, startBeat, endBeat, cutLengthBeat);
    }

    /// <inheritdoc/>
    public List<Rpe.Event<TPayload>> CutEventToLiner(
        Rpe.Event<TPayload> evt, double cutLength)
        => CutEventToLiner(evt, new Beat(cutLength));

    /// <inheritdoc/>
    public List<Rpe.Event<TPayload>> CutEventToLiner(
        Rpe.Event<TPayload> evt,
        Beat cutLength)
    {
        var cutEvents = new List<Rpe.Event<TPayload>>();
        // 在evt中均匀采样，并返回
        var nowBeat = evt.StartBeat;
        while (nowBeat < evt.EndBeat)
        {
            var segmentEnd = nowBeat + cutLength;
            if (segmentEnd > evt.EndBeat)
            {
                segmentEnd = evt.EndBeat;
            }

            cutEvents.Add(new Rpe.Event<TPayload>()
            {
                StartBeat = nowBeat,
                EndBeat = segmentEnd,
                StartValue = evt.GetValueAtBeat(nowBeat),
                EndValue = evt.GetValueAtBeat(segmentEnd),
            });

            nowBeat = segmentEnd;
        }

        return cutEvents;
    }

    /// <inheritdoc/>
    public List<Rpe.Event<TPayload>> CutEventsInRange(List<Rpe.Event<TPayload>> events, Beat startBeat, Beat endBeat,
        Beat cutLength)
    {
        var cutEvents = new List<Rpe.Event<TPayload>>();
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd = evt.EndBeat > endBeat ? endBeat : evt.EndBeat;

            var totalBeats = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling((totalBeats / cutLength));

            for (var i = 0; i < segmentCount; i++)
            {
                var currentBeat = new Beat(cutStart + (cutLength * i));
                var segmentEnd = new Beat(cutStart + (cutLength * (i + 1)));
                if (segmentEnd > cutEnd) segmentEnd = cutEnd;

                cutEvents.Add(new Rpe.Event<TPayload>
                {
                    StartBeat = currentBeat,
                    EndBeat = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue = evt.GetValueAtBeat(segmentEnd),
                });
            }
        }

        return cutEvents;
    }
}