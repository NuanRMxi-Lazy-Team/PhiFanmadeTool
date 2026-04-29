namespace KaedePhi.Tool.Event;

public interface IEventCutter<TEvent, in TBeat>
{
    /// <summary>
    /// 将事件列表中，在指定节拍范围内切割为等长事件。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="startBeat">起始拍</param>
    /// <param name="endBeat">结束拍</param>
    /// <param name="cutLength">切割长度</param>
    /// <returns>处理后的事件列表</returns>
    List<TEvent> CutEventsInRange(
        List<TEvent> events,
        TBeat startBeat,
        TBeat endBeat,
        TBeat cutLength);

    /// <summary>
    /// 将事件列表中，在指定节拍范围内切割为等长事件。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="startBeat">起始拍</param>
    /// <param name="endBeat">结束拍</param>
    /// <param name="cutLength">切割长度</param>
    /// <returns>处理后的事件列表</returns>
    List<TEvent> CutEventsInRange(
        List<TEvent> events,
        TBeat startBeat,
        TBeat endBeat,
        double cutLength);

    /// <summary>
    /// 将事件切割为多个等长事件。
    /// </summary>
    /// <param name="evt">事件</param>
    /// <param name="cutLength">切割长度</param>
    /// <returns>处理后的事件列表</returns>
    List<TEvent> CutEventToLiner(
        TEvent evt,
        TBeat cutLength);

    /// <summary>
    /// 将事件切割为多个等长事件。
    /// </summary>
    /// <param name="evt">事件</param>
    /// <param name="cutLength">切割长度</param>
    /// <returns>处理后的事件列表</returns>
    List<TEvent> CutEventToLiner(
        TEvent evt,
        double cutLength);
}