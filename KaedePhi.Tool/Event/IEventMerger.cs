namespace KaedePhi.Tool.Event;

public interface IEventMerger<TEvent>
{
    /// <summary>
    /// 最普通最暴力算法的事件列表拟合算法，将所有事件切成等长事件，然后逐段相加。
    /// </summary>
    /// <param name="toEvents">从事件列表</param>
    /// <param name="fromEvents">到事件列表</param>
    /// <param name="precision">采样精度，数值越大，采样越细</param>
    /// <returns>经过合并的事件列表，行为不变</returns>
    List<TEvent> EventListMerge(
        List<TEvent>? toEvents,
        List<TEvent>? fromEvents,
        double precision);

    /// <summary>
    /// 性能更优的，基于自适应算法的事件列表拟合算法，同时采样，同时计算误差百分比，天然直接压缩事件数量。
    /// </summary>
    /// <param name="toEvents">从事件列表</param>
    /// <param name="fromEvents">到事件列表</param>
    /// <param name="precision">采样精度，数值越大，采样越细</param>
    /// <param name="tolerance">误差百分比</param>
    /// <returns>经过合并的事件列表，行为不变</returns>
    List<TEvent> EventMergePlus(
        List<TEvent>? toEvents,
        List<TEvent>? fromEvents,
        double precision,
        double tolerance);

}