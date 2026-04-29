using KaedePhi.Core.Common;
using KaedePhi.Tool.KaedePhi.Events.Internal;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;

namespace KaedePhi.Tool.KaedePhi.Layers.Internal;

/// <summary>
/// NRC 层级处理器：合并、切割与清理事件层级。
/// </summary>
internal static class LayerProcessor
{
    /// <summary>移除无用层级（所有事件都为默认值的层级）。</summary>
    internal static List<EventLayer>? RemoveUnlessLayer(List<EventLayer>? layers)
    {
        if (layers is not { Count: > 1 }) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = EventCompressor.RemoveUselessEvent(layer.AlphaEvents);
            layer.MoveXEvents = EventCompressor.RemoveUselessEvent(layer.MoveXEvents);
            layer.MoveYEvents = EventCompressor.RemoveUselessEvent(layer.MoveYEvents);
            layer.RotateEvents = EventCompressor.RemoveUselessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    /// <summary>将多个事件层级各通道的事件切割到指定精度。</summary>
    internal static List<EventLayer> CutLayerEvents(
        List<EventLayer> layers, double precision = 64d)
    {
        layers.RemoveAll(layer => (object?)layer is null);
        layers = RemoveUnlessLayer(layers) ?? layers;
        return layers.Select(layer => CutLayerEvents(layer, precision)).ToList();
    }

    /// <summary>将单个事件层级各类型事件切割到指定精度。</summary>
    internal static EventLayer CutLayerEvents(
        EventLayer? layer, double precision = 64d)
    {
        if (layer == null) return new EventLayer();

        var cutLength = new Beat(1d / precision);
        var cutEventLayer = new EventLayer();

        if (layer.AlphaEvents is { Count: > 0 })
            cutEventLayer.AlphaEvents = EventCutter.CutEventsInRange(
                layer.AlphaEvents,
                layer.AlphaEvents.Min(e => e.StartBeat),
                layer.AlphaEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveXEvents is { Count: > 0 })
            cutEventLayer.MoveXEvents = EventCutter.CutEventsInRange(
                layer.MoveXEvents,
                layer.MoveXEvents.Min(e => e.StartBeat),
                layer.MoveXEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveYEvents is { Count: > 0 })
            cutEventLayer.MoveYEvents = EventCutter.CutEventsInRange(
                layer.MoveYEvents,
                layer.MoveYEvents.Min(e => e.StartBeat),
                layer.MoveYEvents.Max(e => e.EndBeat), cutLength);

        if (layer.RotateEvents is { Count: > 0 })
            cutEventLayer.RotateEvents = EventCutter.CutEventsInRange(
                layer.RotateEvents,
                layer.RotateEvents.Min(e => e.StartBeat),
                layer.RotateEvents.Max(e => e.EndBeat), cutLength);

        if (layer.SpeedEvents is { Count: > 0 })
            cutEventLayer.SpeedEvents = EventCutter.CutEventsInRange(
                layer.SpeedEvents,
                layer.SpeedEvents.Min(e => e.StartBeat),
                layer.SpeedEvents.Max(e => e.EndBeat), cutLength);

        return cutEventLayer;
    }

    /// <summary>合并多个事件层级（固定采样）。</summary>
    internal static EventLayer LayerMerge(
        List<EventLayer> layers, double precision = 64d)
    {
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    EventMerger.EventListMerge(mergedLayer.AlphaEvents, layer.AlphaEvents, precision);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    EventMerger.EventListMerge(mergedLayer.MoveXEvents, layer.MoveXEvents, precision);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    EventMerger.EventListMerge(mergedLayer.MoveYEvents, layer.MoveYEvents, precision);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    EventMerger.EventListMerge(mergedLayer.RotateEvents, layer.RotateEvents, precision);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    EventMerger.EventListMerge(mergedLayer.SpeedEvents, layer.SpeedEvents, precision);
        }

        return mergedLayer;
    }

    /// <summary>合并多个事件层级（自适应采样，性能更优）。</summary>
    internal static EventLayer LayerMergePlus(
        List<EventLayer> layers, double precision, double tolerance)
    {
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    EventMerger.EventMergePlus(mergedLayer.AlphaEvents, layer.AlphaEvents, precision, tolerance);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    EventMerger.EventMergePlus(mergedLayer.MoveXEvents, layer.MoveXEvents, precision, tolerance);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    EventMerger.EventMergePlus(mergedLayer.MoveYEvents, layer.MoveYEvents, precision, tolerance);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    EventMerger.EventMergePlus(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    EventMerger.EventMergePlus(mergedLayer.SpeedEvents, layer.SpeedEvents, precision, tolerance);
        }

        return mergedLayer;
    }

    /// <summary>
    /// 将事件层中的事件进行压缩
    /// </summary>
    /// <param name="layer">事件层</param>
    /// <param name="tolerance">容差百分比</param>
    internal static void LayerEventsCompress(
        EventLayer layer, double tolerance)
    {
        if (layer.AlphaEvents is { Count: > 0 })
            layer.AlphaEvents = EventCompressor.EventListCompressSlope(layer.AlphaEvents, tolerance);
        if (layer.MoveXEvents is { Count: > 0 })
            layer.MoveXEvents = EventCompressor.EventListCompressSqrt(layer.MoveXEvents, tolerance);
        if (layer.MoveYEvents is { Count: > 0 })
            layer.MoveYEvents = EventCompressor.EventListCompressSqrt(layer.MoveYEvents, tolerance);
        if (layer.RotateEvents is { Count: > 0 })
            layer.RotateEvents = EventCompressor.EventListCompressSlope(layer.RotateEvents, tolerance);
        if (layer.SpeedEvents is { Count: > 0 })
            layer.SpeedEvents = EventCompressor.EventListCompressSlope(layer.SpeedEvents, tolerance);
    }
}