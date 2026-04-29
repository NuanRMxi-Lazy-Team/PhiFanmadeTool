using KaedePhi.Core.Common;
using KaedePhi.Tool.KaedePhi.Events.Internal;
using BpmItem = KaedePhi.Core.KaedePhi.BpmItem;

namespace KaedePhi.Tool.KaedePhi.Events;

/// <summary>
/// NRC 格式事件操作工具。提供事件列表的压缩与合并功能。
/// </summary>
public static class KpcEventTools
{
    /// <summary>
    /// 在指定拍范围内将事件按固定拍长切割。
    /// </summary>
    public static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength)
        => EventCutter.CutEventsInRange(events, startBeat, endBeat, cutLength);

    /// <summary>
    /// 在指定拍范围内将事件按固定拍长切割。
    /// </summary>
    public static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
        => EventCutter.CutEventsInRange(events, startBeat, endBeat, cutLength);

    /// <summary>
    /// 将单个事件切割为固定拍长线性事件
    /// </summary>
    public static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt,
        Beat cutLength)
        => EventCutter.CutEventToLiner(evt, cutLength);

    /// <summary>
    /// 将单个事件切割为固定拍长线性事件
    /// </summary>
    public static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt,
        double cutLength)
        => EventCutter.CutEventToLiner(evt, cutLength);

    /// <summary>
    /// 对事件列表做缓动拟合；仅会拟合连续线性事件，原有非线性事件会被保留。
    /// </summary>
    public static List<Kpc.Event<T>> EventListFit<T>(
        List<Kpc.Event<T>> events,
        double tolerance = 5d)
        => EventFit.EventListFit(events, tolerance);

    /// <summary>
    /// 对事件列表做缓动拟合（多核版）；maxDegreeOfParallelism 为并行线程数。
    /// </summary>
    public static List<Kpc.Event<T>> EventListFit<T>(
        List<Kpc.Event<T>> events,
        double tolerance,
        int? maxDegreeOfParallelism)
        => EventFit.EventListFit(events, tolerance, maxDegreeOfParallelism);

    /// <summary>
    /// 对事件列表做缓动拟合（异步版）。
    /// </summary>
    [Obsolete("异步版不再支持",true)]
    public static Task<List<Kpc.Event<T>>> EventListFitAsync<T>(
        List<Kpc.Event<T>> events,
        double tolerance = 5d,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Async version after now to be removed");


    /// <summary>根据容差压缩事件列表，合并变化率相近的相邻线性事件。</summary>
    [Obsolete("已废弃，请改用 EventListCompressSqrt 或 EventListCompressSlope 方法，EventListCompress 方法将被移除。")]
    public static List<Kpc.Event<T>> EventListCompress<T>(
        List<Kpc.Event<T>> events, double tolerance = 5)
        => EventCompressor.EventListCompressSqrt(events, tolerance);

    /// <summary>
    /// 使用了欧几里得距离做算法，对空间更敏感，适用于移动类事件
    /// </summary>
    /// <param name="events"></param>
    /// <param name="tolerance"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [Obsolete("此方法弃用，请使用KaedePhi.Tool.Event.KaedePhi.EventCompressor<T>中的EventListCompressSqrt", false)]
    public static List<Kpc.Event<T>> EventListCompressSqrt<T>(List<Kpc.Event<T>> events, double tolerance = 5)
        => EventCompressor.EventListCompressSqrt(events, tolerance);
    /// <summary>
    /// 使用了普通变化率算法的压缩算法，适用于非移动类事件
    /// </summary>
    /// <param name="events"></param>
    /// <param name="tolerance"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [Obsolete("此方法弃用，请使用KaedePhi.Tool.Event.KaedePhi.EventCompressor<T>中的EventListCompressSlope", false)]
    public static List<Kpc.Event<T>> EventListCompressSlope<T>(List<Kpc.Event<T>> events, double tolerance = 5)
        => EventCompressor.EventListCompressSlope(events, tolerance);

    /// <summary>
    /// 将两个事件列表合并（固定采样策略）。有重叠区间时按等长切片逐段相加，可选压缩。
    /// </summary>
    public static List<Kpc.Event<T>> EventListMerge<T>(
        List<Kpc.Event<T>> toEvents, List<Kpc.Event<T>> fromEvents,
        double precision = 64d)
        => EventMerger.EventListMerge(toEvents, fromEvents, precision);

    /// <summary>
    /// 将两个事件列表合并（自适应采样策略）。性能更优，天然压缩，不支持禁用压缩。
    /// </summary>
    public static List<Kpc.Event<T>> EventMergePlus<T>(
        List<Kpc.Event<T>> toEvents, List<Kpc.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d)
        => EventMerger.EventMergePlus(toEvents, fromEvents, precision, tolerance);

    public static double GetMsAtBeat(Beat beat, List<BpmItem> bpmList,float bpmFactor = 1f)
    {
        var sortedBpms = bpmList.OrderBy(b => (double)b.StartBeat).ToList();
        double ms = 0;

        for (var i = 0; i < sortedBpms.Count; i++)
        {
            var segmentStart = sortedBpms[i].StartBeat;
            if (segmentStart >= beat) break;

            var segmentEnd = i + 1 < sortedBpms.Count && sortedBpms[i + 1].StartBeat < beat
                ? sortedBpms[i + 1].StartBeat
                : beat;

            var beatLength = (double)(segmentEnd - segmentStart);
            ms += beatLength / sortedBpms[i].Bpm / bpmFactor * 60000d;
        }

        return ms;
    }
}