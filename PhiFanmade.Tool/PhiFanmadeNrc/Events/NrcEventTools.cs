using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events;

/// <summary>
/// NRC 格式事件操作工具。提供事件列表的压缩与合并功能。
/// </summary>
public static class NrcEventTools
{
    /// <summary>根据容差压缩事件列表，合并变化率相近的相邻线性事件。</summary>
    public static List<Nrc.Event<double>> EventListCompress(
        List<Nrc.Event<double>> events, double tolerance = 5)
        => EventCompressor.EventListCompress(events, tolerance);

    /// <summary>
    /// 将两个事件列表合并（固定采样策略）。有重叠区间时按等长切片逐段相加，可选压缩。
    /// </summary>
    public static List<Nrc.Event<T>> EventListMerge<T>(
        List<Nrc.Event<T>> toEvents, List<Nrc.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => EventMerger.EventListMerge(toEvents, fromEvents, precision, tolerance, compress);

    /// <summary>
    /// 将两个事件列表合并（自适应采样策略）。性能更优，天然压缩，不支持禁用压缩。
    /// </summary>
    public static List<Nrc.Event<T>> EventMergePlus<T>(
        List<Nrc.Event<T>> toEvents, List<Nrc.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d)
        => EventMerger.EventMergePlus(toEvents, fromEvents, precision, tolerance);
}