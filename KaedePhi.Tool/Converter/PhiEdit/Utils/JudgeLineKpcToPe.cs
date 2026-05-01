using KaedePhi.Tool.Converter.PhiEdit.Model;
using global::KaedePhi.Tool.JudgeLines.KaedePhi;
using AlphaControl = KaedePhi.Core.KaedePhi.AlphaControl;
using ExtendLayer = KaedePhi.Core.KaedePhi.ExtendLayer;
using KpcJudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using SizeControl = KaedePhi.Core.KaedePhi.SizeControl;
using SkewControl = KaedePhi.Core.KaedePhi.SkewControl;
using XControl = KaedePhi.Core.KaedePhi.XControl;
using YControl = KaedePhi.Core.KaedePhi.YControl;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// KPC 判定线到 PE 判定线的转换器。
/// </summary>
public class JudgeLineKpcToPe
{
    private const float FloatEpsilon = 1e-6f;
    private readonly KpcToPhiEditConvertOptions _options;
    private readonly LineEventBuilder _eventBuilder;
    private readonly Action<string> _warnLogger;

    public JudgeLineKpcToPe(KpcToPhiEditConvertOptions options, Action<string> warnLogger)
    {
        _options = options;
        _eventBuilder = new LineEventBuilder(options);
        _warnLogger = warnLogger;
    }

    /// <summary>
    /// 转换单条判定线，并在转换前记录 PE 不支持字段的告警。
    /// </summary>
    public Pe.JudgeLine ConvertJudgeLine(KpcJudgeLine src, List<KpcJudgeLine> allLine)
    {
        WarnIfUnsupportedJudgeLineFields(src);
        var trueSrc = src;
        var pe = new Pe.JudgeLine
        {
            NoteList = trueSrc.Notes?.ConvertAll(n => Note.ConvertNote(n, _warnLogger)) ?? []
        };

        if (!string.Equals(trueSrc.Texture, "line.png", StringComparison.Ordinal) ||
            _options.LineFilter.RemoveTextureLine || trueSrc.AttachUi.HasValue ||
            _options.LineFilter.RemoveAttachUiLine)
        {
            return pe;
        }

        if (trueSrc.Father != -1)
        {
            Warn($"PE 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
            var unbinder = new KpcJudgeLineUnbinder();
            if (_options.FatherLineUnbind.ClassicMode)
            {
                trueSrc = unbinder.FatherUnbind(allLine.FindIndex(l => l.GetHashCode() == src.GetHashCode()),
                    allLine, _options.FatherLineUnbind.Precision);
            }
            else
                trueSrc = unbinder.FatherUnbindPlus(
                    allLine.FindIndex(l => l.GetHashCode() == src.GetHashCode()),
                    allLine, _options.FatherLineUnbind.Precision, _options.FatherLineUnbind.Tolerance);
        }

        _eventBuilder.ConvertLineEvents(pe, trueSrc.EventLayers ?? []);
        if (pe.AlphaEvents.Count == 0 && pe.AlphaFrames.Count == 0)
            pe.AlphaFrames.Add(new Pe.Frame());

        return pe;
    }

    /// <summary>
    /// 检查判定线中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private void WarnIfUnsupportedJudgeLineFields(KpcJudgeLine src)
    {
        if (!string.Equals(src.Name, "Untitled", StringComparison.Ordinal))
            Warn($"PE 不支持 JudgeLine.Name（值='{src.Name}'）");
        if (!string.Equals(src.Texture, "line.png", StringComparison.Ordinal))
            Warn(
                $"PE 不支持 JudgeLine.Texture（值='{src.Texture}'），{(_options.LineFilter.RemoveTextureLine ? "，判定线将被自动移除。" : "。")}");
        if (!IsDefaultAnchor(src.Anchor))
            Warn($"PE 不支持 JudgeLine.Anchor（值='[{string.Join(", ", src.Anchor)}]'）");
        if (src.Father != -1)
            Warn($"PE 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
        if (!src.IsCover)
            Warn($"PE 不支持 JudgeLine.IsCover（值={src.IsCover}）");
        if (src.ZOrder != 0)
            Warn($"PE 不支持 JudgeLine.ZOrder（值={src.ZOrder}）");
        if (src.AttachUi.HasValue)
            Warn(
                $"PE 不支持 JudgeLine.AttachUi（值={(int)src.AttachUi.Value}）{(_options.LineFilter.RemoveAttachUiLine ? "，判定线将被自动移除。" : "。")}"
            );
        if (src.IsGif)
            Warn($"PE 不支持 JudgeLine.IsGif（值={src.IsGif}）");
        if (Math.Abs(src.BpmFactor - 1f) > FloatEpsilon)
            Warn($"PE 不支持 JudgeLine.BpmFactor（值={src.BpmFactor}）");

        if (HasNonDefaultExtendLayer(src.Extended))
            Warn("PE 不支持 JudgeLine.Extended（包含非默认数据）");
        if (!IsDefaultXControls(src.PositionControls))
            Warn("PE 不支持 JudgeLine.PositionControls（包含非默认数据）");
        if (!IsDefaultAlphaControls(src.AlphaControls))
            Warn("PE 不支持 JudgeLine.AlphaControls（包含非默认数据）");
        if (!IsDefaultSizeControls(src.SizeControls))
            Warn("PE 不支持 JudgeLine.SizeControls（包含非默认数据）");
        if (!IsDefaultSkewControls(src.SkewControls))
            Warn("PE 不支持 JudgeLine.SkewControls（包含非默认数据）");
        if (!IsDefaultYControls(src.YControls))
            Warn("PE 不支持 JudgeLine.YControls（包含非默认数据）");
    }

    private static bool HasNonDefaultExtendLayer(ExtendLayer? layer)
        => layer != null
           && ((layer.ColorEvents?.Count ?? 0) > 0
               || (layer.ScaleXEvents?.Count ?? 0) > 0
               || (layer.ScaleYEvents?.Count ?? 0) > 0
               || (layer.TextEvents?.Count ?? 0) > 0
               || (layer.PaintEvents?.Count ?? 0) > 0
               || (layer.GifEvents?.Count ?? 0) > 0);

    private static bool IsDefaultAnchor(float[]? anchor)
        => anchor is { Length: 2 }
           && Math.Abs(anchor[0] - 0.5f) <= FloatEpsilon
           && Math.Abs(anchor[1] - 0.5f) <= FloatEpsilon;

    private static bool IsDefaultXControls(List<XControl>? controls)
    {
        var def = XControl.Default;
        if (controls == null || controls.Count != def.Count) return false;
        for (var i = 0; i < def.Count; i++)
        {
            var d = def[i];
            var c = controls[i];
            if ((int)d.Easing != (int)c.Easing) return false;
            if (Math.Abs(d.X - c.X) > FloatEpsilon) return false;
            if (Math.Abs(d.Pos - c.Pos) > FloatEpsilon) return false;
        }

        return true;
    }

    private static bool IsDefaultAlphaControls(List<AlphaControl>? controls)
    {
        var def = AlphaControl.Default;
        if (controls == null || controls.Count != def.Count) return false;
        for (var i = 0; i < def.Count; i++)
        {
            var d = def[i];
            var c = controls[i];
            if ((int)d.Easing != (int)c.Easing) return false;
            if (Math.Abs(d.X - c.X) > FloatEpsilon) return false;
            if (Math.Abs(d.Alpha - c.Alpha) > FloatEpsilon) return false;
        }

        return true;
    }

    private static bool IsDefaultSizeControls(List<SizeControl>? controls)
    {
        var def = SizeControl.Default;
        if (controls == null || controls.Count != def.Count) return false;
        for (var i = 0; i < def.Count; i++)
        {
            var d = def[i];
            var c = controls[i];
            if ((int)d.Easing != (int)c.Easing) return false;
            if (Math.Abs(d.X - c.X) > FloatEpsilon) return false;
            if (Math.Abs(d.Size - c.Size) > FloatEpsilon) return false;
        }

        return true;
    }

    private static bool IsDefaultSkewControls(List<SkewControl>? controls)
    {
        var def = SkewControl.Default;
        if (controls == null || controls.Count != def.Count) return false;
        for (var i = 0; i < def.Count; i++)
        {
            var d = def[i];
            var c = controls[i];
            if ((int)d.Easing != (int)c.Easing) return false;
            if (Math.Abs(d.X - c.X) > FloatEpsilon) return false;
            if (Math.Abs(d.Skew - c.Skew) > FloatEpsilon) return false;
        }

        return true;
    }

    private static bool IsDefaultYControls(List<YControl>? controls)
    {
        var def = YControl.Default;
        if (controls == null || controls.Count != def.Count) return false;
        for (var i = 0; i < def.Count; i++)
        {
            var d = def[i];
            var c = controls[i];
            if ((int)d.Easing != (int)c.Easing) return false;
            if (Math.Abs(d.X - c.X) > FloatEpsilon) return false;
            if (Math.Abs(d.Y - c.Y) > FloatEpsilon) return false;
        }

        return true;
    }


    private void Warn(string message) => _warnLogger?.Invoke(message);
}