using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using global::KaedePhi.Tool.KaedePhi;
using Meta = KaedePhi.Core.KaedePhi.Meta;

namespace KaedePhi.Tool.Converter.PhiEdit;

/// <summary>
/// PhiEdit Converter.
/// ToKpc: TInOptions = Unit? (unused).
/// FromKpc: TOutOptions = PhiEditConvertOptions.
/// </summary>
public class PhiEditConverter : IChartConverter<Pe.Chart, Unit?, PhiEditConvertOptions>
{
    public Kpc.Chart ToKpc(Pe.Chart source, Unit? _ = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new Kpc.Chart
        {
            BpmList = source.BpmList?.ConvertAll(Utils.BpmItem.ConvertBpmItem) ?? [],
            Meta = Utils.Meta.ConvertMeta(source),
            JudgeLineList = Utils.JudgeLineConverter.ConvertJudgeLines(source.JudgeLineList)
        };
    }

    public Pe.Chart FromKpc(Kpc.Chart input, PhiEditConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        
        WarnIfUnsupportedMeta(input.Meta);

        var judgeLineConverter = new Utils.JudgeLineKpcToPe(options);

        return new Pe.Chart
        {
            Offset = Utils.Meta.GetPeOffset(input.Meta),
            BpmList = input.BpmList?.ConvertAll(Utils.BpmItem.ConvertBpmItem) ?? [],
            JudgeLineList = input.JudgeLineList?.ConvertAll(j => judgeLineConverter.ConvertJudgeLine(j, input.JudgeLineList)) ?? []
        };
    }

    private static void WarnIfUnsupportedMeta(Meta src)
    {
        var defaults = new Meta();
        if (src.Background != defaults.Background)
            Warn($"PE 不支持 Meta.Background（值='{src.Background}'）");
        if (src.Author != defaults.Author) Warn($"PE 不支持 Meta.Author（值='{src.Author}'）");
        if (src.Composer != defaults.Composer) Warn($"PE 不支持 Meta.Composer（值='{src.Composer}'）");
        if (src.Artist != defaults.Artist) Warn($"PE 不支持 Meta.Artist（值='{src.Artist}'）");
        if (src.Level != defaults.Level) Warn($"PE 不支持 Meta.Level（值='{src.Level}'）");
        if (src.Name != defaults.Name) Warn($"PE 不支持 Meta.Name（值='{src.Name}'）");
        if (src.Song != defaults.Song) Warn($"PE 不支持 Meta.Song（值='{src.Song}'）");
    }

    private static void WarnIfUnsupportedNoteFields(Kpc.Note src)
    {
        if (src.Alpha != 255)
            Warn($"PE 不支持 Note.Alpha（值={src.Alpha}）");
        if (Math.Abs(src.JudgeArea - 1f) > 1e-6f)
            Warn($"PE 不支持 Note.JudgeArea（值={src.JudgeArea}）");
        if (Math.Abs(src.VisibleTime - 999999f) > 1e-6f)
            Warn($"PE 不支持 Note.VisibleTime（值={src.VisibleTime}）");
        if (Math.Abs(src.YOffset) > 1e-6f)
            Warn($"PE 不支持 Note.YOffset（值={src.YOffset}）");
        if (!IsDefaultTint(src.Tint))
            Warn($"PE 不支持 Note.Tint（值='[{string.Join(", ", src.Tint)}]'）");
        if (src.HitFxColor != null)
            Warn($"PE 不支持 Note.HitFxColor（值='[{string.Join(", ", src.HitFxColor)}]'）");
        if (!string.IsNullOrWhiteSpace(src.HitSound))
            Warn($"PE 不支持 Note.HitSound（值='{src.HitSound}'）");
    }

    private static bool IsDefaultTint(byte[]? tint) => tint is [255, 255, 255];

    private static void Warn(string message) => KpcToolLog.OnWarning($"[ToPe] {message}");
}
