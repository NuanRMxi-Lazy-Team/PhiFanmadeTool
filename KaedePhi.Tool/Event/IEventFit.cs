namespace KaedePhi.Tool.Event;

public interface IEventFit<TEvent>
{
    /// <summary>
    /// 将事件列表中存在的线性事件拟合为有缓动函数的事件
    /// </summary>
    /// <param name="events"></param>
    /// <param name="tolerance"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    List<TEvent> EventListFit(
        List<TEvent>? events,
        double tolerance);

    /// <summary>
    /// 将事件列表中存在的线性事件拟合为有缓动函数的事件（可指定最大并行度）
    /// </summary>
    /// <param name="events"></param>
    /// <param name="tolerance"></param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    List<TEvent> EventListFit(
        List<TEvent>? events,
        double tolerance,
        int? maxDegreeOfParallelism);
}