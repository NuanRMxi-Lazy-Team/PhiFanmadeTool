namespace PhiFanmade.Tool.Utils.RePhiEditUtility;

/// <summary>
/// RePhiEdit层级处理器
/// </summary>
internal static class LayerProcessor
{
    /// <summary>
    /// 移除无用层级（所有事件都为默认值的层级）
    /// </summary>
    internal static List<Rpe.EventLayer>? RemoveUnlessLayer(List<Rpe.EventLayer>? layers)
    {
        if (layers is not { Count: > 1 }) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        // index非1 layer头部的0值事件去除
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = EventProcessor.RemoveUnlessEvent(layer.AlphaEvents);
            layer.MoveXEvents = EventProcessor.RemoveUnlessEvent(layer.MoveXEvents);
            layer.MoveYEvents = EventProcessor.RemoveUnlessEvent(layer.MoveYEvents);
            layer.RotateEvents = EventProcessor.RemoveUnlessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    /// <summary>
    /// 合并多个事件层级
    /// </summary>
    public static Rpe.EventLayer LayerMerge(List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
            return layers.FirstOrDefault() ?? new Rpe.EventLayer();
        

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new Rpe.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    EventProcessor.EventMerge(mergedLayer.AlphaEvents, layer.AlphaEvents, precision, tolerance);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    EventProcessor.EventMerge(mergedLayer.MoveXEvents, layer.MoveXEvents, precision, tolerance);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    EventProcessor.EventMerge(mergedLayer.MoveYEvents, layer.MoveYEvents, precision, tolerance);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    EventProcessor.EventMerge(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    EventProcessor.EventMerge(mergedLayer.SpeedEvents, layer.SpeedEvents, precision, tolerance);
        }

        return mergedLayer;
    }
}