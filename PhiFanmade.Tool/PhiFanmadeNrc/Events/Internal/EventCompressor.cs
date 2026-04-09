namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件压缩器：合并变化率相近的相邻线性事件，以及移除无意义的默认值事件。
/// </summary>
internal static class EventCompressor
{
    /// <summary>
    /// 压缩事件列表，合并相连的线性事件。
    /// 使用归一化 (拍, 值) 空间中的垂直距离度量误差：
    /// 将两段合并为一段后，在原交界点处计算归一化垂直距离是否在容差之内。
    /// 与原来的斜率比较方法相比，本算法对长段误差更敏感，且不受坐标系 X/Y 轴缩放影响。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">拟合容差百分比，越大拟合精细度越低</param>
    internal static List<Nrc.Event<T>> EventListCompress<T>(
        List<Nrc.Event<T>>? events, double tolerance = 5)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListCompress only supports int, float, and double types.");

        if (events == null || events.Count == 0) return [];

        var compressed = new List<Nrc.Event<T>> { events[0] };

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1 &&
                lastEvent.EndBeat == currentEvent.StartBeat)
            {
                var tA = (double)lastEvent.StartBeat;
                var tB = (double)lastEvent.EndBeat;
                var tC = (double)currentEvent.EndBeat;
                var vA     = Convert.ToDouble(lastEvent.StartValue);
                var vBend  = Convert.ToDouble(lastEvent.EndValue);
                var vBstart = Convert.ToDouble(currentEvent.StartValue);
                var vC     = Convert.ToDouble(currentEvent.EndValue);

                // 归一化比例尺：所有端点绝对值的最大值。
                // 下限设为 1e-3（NRC 坐标全范围 [-1,1] 的 0.1%），避免坐标趋零时
                // 浮点残差被放大 10^6 倍导致 perpDist 爆炸、永远无法合并。
                var scale  = Math.Max(Math.Max(Math.Abs(vA), Math.Abs(vBend)),
                                      Math.Max(Math.Abs(vC), 1e-3));
                var relTol = tolerance / 100.0;

                // 检查交界处的连续性：在归一化值域中，两段交界点的间距需在容差内
                var normalizedGap = Math.Abs(vBend - vBstart) / scale;

                if (normalizedGap <= relTol)
                {
                    var tSpan = tC - tA;
                    bool canMerge;

                    if (tSpan < 1e-12)
                    {
                        // 合并后长度趋零，直接合并
                        canMerge = true;
                    }
                    else
                    {
                        // 在归一化 (拍, 值) 空间中计算交界点到合并段的垂直距离：
                        //   归一化后 A'=(0,0)，C'=(1, dvNorm)，B'=(tNorm, byNorm)
                        //   垂直距离 d = |byNorm − dvNorm·tNorm| / sqrt(1 + dvNorm²)
                        var tNorm  = (tB - tA) / tSpan;
                        var dvNorm = (vC    - vA) / scale;
                        var byNorm = (vBend - vA) / scale;
                        var det    = byNorm - dvNorm * tNorm;
                        var len    = Math.Sqrt(1.0 + dvNorm * dvNorm);
                        var perpDist = Math.Abs(det) / len;
                        canMerge = perpDist <= relTol;
                    }

                    if (canMerge)
                    {
                        lastEvent.EndBeat  = currentEvent.EndBeat;
                        lastEvent.EndValue = currentEvent.EndValue;
                        continue;
                    }
                }
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 移除无用事件（起始值和结束值都为默认值的事件）。
    /// </summary>
    internal static List<Nrc.Event<T>>? RemoveUselessEvent<T>(List<Nrc.Event<T>>? events)
    {
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy is { Count: 1 } &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue, default))
        {
            eventsCopy.RemoveAt(0);
        }

        return eventsCopy;
    }
}