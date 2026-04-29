namespace KaedePhi.Tool.Event;

public interface IEventCompressor<TEvent>
{
    /// <summary>
    /// 使用归一化垂直距离算法对事件列表进行压缩
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>压缩后的事件列表</returns>
    List<TEvent> EventListCompressSqrt(List<TEvent>? events, double tolerance);
    /// <summary>
    /// 使用归一化斜率算法对事件列表进行压缩
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>压缩后的事件列表</returns>
    List<TEvent> EventListCompressSlope(List<TEvent>? events, double tolerance);
}