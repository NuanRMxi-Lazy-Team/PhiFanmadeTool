using KaedePhi.Core.Common;
using PhigrosEvent = KaedePhi.Core.Phigros.v3.Event;
using PhigrosJudgeLine = KaedePhi.Core.Phigros.v3.JudgeLine;
using PhigrosNoteType = KaedePhi.Core.Phigros.v3.NoteType;
using PhigrosSpeedEvent = KaedePhi.Core.Phigros.v3.SpeedEvent;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class Event
{
    private const double TrailingBeatPadding = 1d / 64d;
    private const double SpeedValueRatio = 4.5d;

    public static List<Kpc.Event<T>>? ConvertEvents<T>(
        List<PhigrosEvent>? events,
        double horizonBeat,
        Func<float, T> valueTransformer)
    {
        if (events is not { Count: > 0 }) return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<Kpc.Event<T>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            if (endBeat <= startBeat) continue;

            var startValue = valueTransformer(ev.Start);
            var endValue = valueTransformer(ev.End);
            if (EqualityComparer<T>.Default.Equals(startValue, endValue) && endBeat - startBeat > 1d)
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, startValue, endValue));
        }

        if (result.Count == 0) return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static List<Kpc.Event<double>>? ConvertMoveAxisEvents(
        List<PhigrosEvent>? events,
        double horizonBeat,
        Func<PhigrosEvent, float> startSelector,
        Func<PhigrosEvent, float> endSelector)
    {
        if (events is not { Count: > 0 }) return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<Kpc.Event<double>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            if (endBeat <= startBeat) continue;

            var startValue = Transform.ToKpcX(startSelector(ev));
            var endValue = Transform.ToKpcX(endSelector(ev));
            if (startValue == endValue && endBeat - startBeat > 1d)
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, startValue, endValue));
        }

        if (result.Count == 0) return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static List<Kpc.Event<float>>? ConvertSpeedEvents(
        List<PhigrosSpeedEvent>? events,
        double horizonBeat)
    {
        if (events is not { Count: > 0 }) return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<Kpc.Event<float>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            var speedValue = (float)(ev.Value * SpeedValueRatio);
            if (endBeat <= startBeat) continue;

            if (endBeat - startBeat > 1d)
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, speedValue, speedValue));
        }

        if (result.Count == 0) return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static double GetJudgeLineHorizonBeat(PhigrosJudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesAbove?.Select(n => (double)n.Time / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesBelow?.Select(n => (double)n.Time / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesAbove?
            .Where(n => n.Type == PhigrosNoteType.Hold)
            .Select(n => (double)(n.Time + n.HoldTime) / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesBelow?
            .Where(n => n.Type == PhigrosNoteType.Hold)
            .Select(n => (double)(n.Time + n.HoldTime) / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.SpeedEvents?.Select(e =>
            Math.Min((double)e.EndTime / 32, Math.Max(0d, (double)e.StartTime / 32) + 1))));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.JudgeLineMoveEvents?.Select(e =>
            e.Start == e.End ? Math.Min((double)e.EndTime / 32, Math.Max(0d, (double)e.StartTime / 32) + 1) : (double)e.EndTime / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.JudgeLineRotateEvents?.Select(e =>
            e.Start == e.End ? Math.Min((double)e.EndTime / 32, Math.Max(0d, (double)e.StartTime / 32) + 1) : (double)e.EndTime / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.JudgeLineDisappearEvents?.Select(e =>
            e.Start == e.End ? Math.Min((double)e.EndTime / 32, Math.Max(0d, (double)e.StartTime / 32) + 1) : (double)e.EndTime / 32)));
        return maxBeat + TrailingBeatPadding;
    }

    private static Kpc.Event<T> CreateLinearEvent<T>(double startBeat, double endBeat, T startValue, T endValue)
        => new()
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue
        };

    private static void AddTrailingEvent<T>(List<Kpc.Event<T>> events, double horizonBeat)
    {
        var lastEnd = (double)events[^1].EndBeat;
        var trailingEnd = Math.Max(horizonBeat, lastEnd + TrailingBeatPadding);
        if (trailingEnd <= lastEnd) return;

        events.Add(CreateLinearEvent(lastEnd, trailingEnd, events[^1].EndValue, events[^1].EndValue));
    }

    private static double GetMaxBeat(IEnumerable<double>? beats) => beats?.DefaultIfEmpty(0d).Max() ?? 0d;
}
