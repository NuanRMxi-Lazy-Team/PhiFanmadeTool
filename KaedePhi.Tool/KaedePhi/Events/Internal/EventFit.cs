using KaedePhi.Core.Common;
using Easing = KaedePhi.Core.KaedePhi.Easing;

namespace KaedePhi.Tool.KaedePhi.Events.Internal;

/// <summary>
/// NRC 事件拟合器。
/// 新模型：在容差约束下，通过有界动态规划寻找较优分段，并对每段做缓动回归。
/// </summary>
internal static class EventFit
{
    /// <summary>
    /// 对事件列表执行同步拟合。
    /// </summary>
    [Obsolete("请使用global::KaedePhi.Tool.Event.KaedePhi.EventFit<T>().EventListFit")]
    internal static List<Kpc.Event<T>> EventListFit<T>(
        List<Kpc.Event<T>>? events,
        double tolerance = 5d)
        => new global::KaedePhi.Tool.Event.KaedePhi.EventFit<T>().EventListFit(events, tolerance);

    /// <summary>
    /// 对事件列表执行同步拟合（可指定并行度）。
    /// </summary>
    [Obsolete("请使用global::KaedePhi.Tool.Event.KaedePhi.EventFit<T>().EventListFit")]
    internal static List<Kpc.Event<T>> EventListFit<T>(
        List<Kpc.Event<T>>? events,
        double tolerance,
        int? maxDegreeOfParallelism)
        => new global::KaedePhi.Tool.Event.KaedePhi.EventFit<T>().EventListFit(events, tolerance,maxDegreeOfParallelism);

    /// <summary>
    /// 对事件列表执行异步拟合。
    /// </summary>
    [Obsolete("异步版不再支持",true)]
    internal static Task<List<Kpc.Event<T>>> EventListFitAsync<T>(
        List<Kpc.Event<T>>? events,
        double tolerance = 5d,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Async version after now to be removed");
}