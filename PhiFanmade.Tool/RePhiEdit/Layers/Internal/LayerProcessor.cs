using PhiFanmade.Core.Common;
using PhiFanmade.Tool.RePhiEdit.Events.Internal;

namespace PhiFanmade.Tool.RePhiEdit.Layers.Internal;

/// <summary>RPE 层级处理器。</summary>
internal static class LayerProcessor
{
    internal static List<Rpe.EventLayer>? RemoveUselessLayer(List<Rpe.EventLayer>? layers)
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

    internal static List<Rpe.EventLayer> CutLayerEvents(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        layers.RemoveAll(l => l == null);
        layers = RemoveUselessLayer(layers) ?? layers;
        return layers.Select(l => CutLayerEvents(l, precision, tolerance, compress)).ToList();
    }

    internal static Rpe.EventLayer CutLayerEvents(
        Rpe.EventLayer? layer, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        if (layer == null) return new Rpe.EventLayer();
        var cutLength = new Beat(1d / precision);
        var cutEventLayer = new Rpe.EventLayer();

        if (layer.AlphaEvents is { Count: > 0 })
            cutEventLayer.AlphaEvents = EventCutter.CutEventsInRange(layer.AlphaEvents,
                layer.AlphaEvents.Min(e => e.StartBeat) ?? new Beat(0), layer.AlphaEvents.Max(e => e.EndBeat), cutLength);
        if (layer.MoveXEvents is { Count: > 0 })
            cutEventLayer.MoveXEvents = EventCutter.CutEventsInRange(layer.MoveXEvents,
                layer.MoveXEvents.Min(e => e.StartBeat), layer.MoveXEvents.Max(e => e.EndBeat), cutLength);
        if (layer.MoveYEvents is { Count: > 0 })
            cutEventLayer.MoveYEvents = EventCutter.CutEventsInRange(layer.MoveYEvents,
                layer.MoveYEvents.Min(e => e.StartBeat), layer.MoveYEvents.Max(e => e.EndBeat), cutLength);
        if (layer.RotateEvents is { Count: > 0 })
            cutEventLayer.RotateEvents = EventCutter.CutEventsInRange(layer.RotateEvents,
                layer.RotateEvents.Min(e => e.StartBeat), layer.RotateEvents.Max(e => e.EndBeat), cutLength);
        if (layer.SpeedEvents is { Count: > 0 })
            cutEventLayer.SpeedEvents = EventCutter.CutEventsInRange(layer.SpeedEvents,
                layer.SpeedEvents.Min(e => e.StartBeat), layer.SpeedEvents.Max(e => e.EndBeat), cutLength);

        if (compress)
        {
            cutEventLayer.AlphaEvents = EventCompressor.EventListCompress(cutEventLayer.AlphaEvents, tolerance);
            cutEventLayer.MoveXEvents = EventCompressor.EventListCompress(cutEventLayer.MoveXEvents, tolerance);
            cutEventLayer.MoveYEvents = EventCompressor.EventListCompress(cutEventLayer.MoveYEvents, tolerance);
            cutEventLayer.RotateEvents = EventCompressor.EventListCompress(cutEventLayer.RotateEvents, tolerance);
            cutEventLayer.SpeedEvents = EventCompressor.EventListCompress(cutEventLayer.SpeedEvents, tolerance);
        }

        return cutEventLayer;
    }

    internal static Rpe.EventLayer LayerMerge(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        layers.RemoveAll(l => (Rpe.EventLayer?)l == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new Rpe.EventLayer();
        layers = RemoveUselessLayer(layers) ?? layers;
        var mergedLayer = new Rpe.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents = EventMerger.EventListMerge(mergedLayer.AlphaEvents, layer.AlphaEvents,
                    precision, tolerance, compress);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents = EventMerger.EventListMerge(mergedLayer.MoveXEvents, layer.MoveXEvents,
                    precision, tolerance, compress);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents = EventMerger.EventListMerge(mergedLayer.MoveYEvents, layer.MoveYEvents,
                    precision, tolerance, compress);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents = EventMerger.EventListMerge(mergedLayer.RotateEvents, layer.RotateEvents,
                    precision, tolerance, compress);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents = EventMerger.EventListMerge(mergedLayer.SpeedEvents, layer.SpeedEvents,
                    precision, tolerance, compress);
        }

        return mergedLayer;
    }

    internal static Rpe.EventLayer LayerMergePlus(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        layers.RemoveAll(l => (Rpe.EventLayer?)l == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new Rpe.EventLayer();
        layers = RemoveUselessLayer(layers) ?? layers;
        var mergedLayer = new Rpe.EventLayer();
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
}