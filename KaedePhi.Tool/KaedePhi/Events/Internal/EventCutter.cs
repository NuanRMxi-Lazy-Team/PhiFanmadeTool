using KaedePhi.Core.Common;

namespace KaedePhi.Tool.KaedePhi.Events.Internal;

/// <summary>
/// NRC 事件切割器：将事件列表按指定拍长切割为等长段。
/// </summary>
internal static class EventCutter
{
    /// <summary>
    /// 在指定的拍范围内切割事件列表。
    /// </summary>
    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>().CutEventsInRange")]
    internal static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength)
    {
        var cutter = new global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>();
        return cutter.CutEventsInRange(events, startBeat, endBeat, cutLength);
    }
    
    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>().CutEventsInRange")]
    internal static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
    {
        var cutter = new global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>();
        return cutter.CutEventsInRange(events, startBeat, endBeat, cutLength);
    }

    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>().CutEventToLiner")]
    internal static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt, double cutLength)
        => CutEventToLiner(evt, new Beat(cutLength));

    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>().CutEventToLiner")]
    internal static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt,
        Beat cutLength)
    {
        var cutter = new global::KaedePhi.Tool.Event.KaedePhi.EventCutter<T>();
        return cutter.CutEventToLiner(evt, cutLength);
    }
}