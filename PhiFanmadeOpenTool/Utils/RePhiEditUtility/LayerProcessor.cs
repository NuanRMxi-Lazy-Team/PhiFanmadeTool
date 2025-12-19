using static PhiFanmade.Core.RePhiEdit.RePhiEdit;
using PhiFanmade.OpenTool.Localization;

namespace PhiFanmade.OpenTool.Utils.RePhiEditUtility;

/// <summary>
/// RePhiEdit层级处理器
/// </summary>
internal static class LayerProcessor
{
    /// <summary>
    /// 移除无用层级（所有事件都为默认值的层级）
    /// </summary>
    internal static List<EventLayer>? RemoveUnlessLayer(List<EventLayer>? layers)
    {
        if (layers == null || layers.Count <= 1) return layers;
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
    public static EventLayer LayerMerge(List<EventLayer> layers)
    {
        var loc = Localizer.Create();
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
        {
            //OnWarning.Invoke("LayerMerge: layers count less than or equal to 1, no need to merge.");
            //RePhiEditUtility.OnWarning.Invoke("LayerMerge" + loc["util.rpe.warn.layer_insufficient_quantity"]);
            return layers.FirstOrDefault() ?? new EventLayer();
        }

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents != null && layer.AlphaEvents.Count > 0)
                mergedLayer.AlphaEvents = EventProcessor.EventMerge(mergedLayer.AlphaEvents, layer.AlphaEvents);
            if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                mergedLayer.MoveXEvents = EventProcessor.EventMerge(mergedLayer.MoveXEvents, layer.MoveXEvents);
            if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                mergedLayer.MoveYEvents = EventProcessor.EventMerge(mergedLayer.MoveYEvents, layer.MoveYEvents);
            if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                mergedLayer.RotateEvents = EventProcessor.EventMerge(mergedLayer.RotateEvents, layer.RotateEvents);
            if (layer.SpeedEvents != null && layer.SpeedEvents.Count > 0)
                mergedLayer.SpeedEvents = EventProcessor.EventMerge(mergedLayer.SpeedEvents, layer.SpeedEvents);
        }

        return mergedLayer;
    }
}

