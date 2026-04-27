using KaedePhi.Tool.Cli.Infrastructure;

namespace KaedePhi.Tool.Cli.Settings.Operation;

/// <summary>
/// 事件通道渲染命令的 CLI 参数
/// </summary>
public abstract class OperationSettingsForRender : OperationSettingsBase
{
    // ── --pixels-per-beat ──────────────────────────────────────────────────

    private float? _pixelsPerBeat;
    private bool _pixelsPerBeatSpecified;

    [CommandOption("-p|--pixels-per-beat <N>")]
    [LocalizedDescription("render_opt_pixels_per_beat")]
    public float PixelsPerBeat
    {
        get => _pixelsPerBeat ?? 100f;
        set { _pixelsPerBeatSpecified = true; _pixelsPerBeat = value; }
    }

    // ── --channel-width ────────────────────────────────────────────────────

    private int? _channelWidth;
    private bool _channelWidthSpecified;

    [CommandOption("--channel-width <N>")]
    [LocalizedDescription("render_opt_channel_width")]
    public int ChannelWidth
    {
        get => _channelWidth ?? 150;
        set { _channelWidthSpecified = true; _channelWidth = value; }
    }

    // ── --samples ──────────────────────────────────────────────────────────

    private int? _samples;
    private bool _samplesSpecified;

    [CommandOption("--samples <N>")]
    [LocalizedDescription("render_opt_samples")]
    public int SamplesPerEvent
    {
        get => _samples ?? 64;
        set { _samplesSpecified = true; _samples = value; }
    }

    // ── --line / --layer ───────────────────────────────────────────────────

    [CommandOption("--line <INDEX>")]
    [LocalizedDescription("render_opt_line")]
    public int? LineIndex { get; set; }

    [CommandOption("--layer <INDEX>")]
    [LocalizedDescription("render_opt_layer")]
    public int? LayerIndex { get; set; }

    // ── --beat-subdivisions ────────────────────────────────────────────────

    private int? _beatSubdivisions;
    private bool _beatSubdivisionsSpecified;

    [CommandOption("-b|--beat-subdivisions <N>")]
    [LocalizedDescription("render_opt_beat_subdivisions")]
    public int BeatSubdivisions
    {
        get => _beatSubdivisions ?? 2;
        set { _beatSubdivisionsSpecified = true; _beatSubdivisions = value; }
    }

    // ── 配置默认值来源（子类覆写）─────────────────────────────────────────

    protected virtual float? GetConfigPixelsPerBeat() => null;
    protected virtual int? GetConfigChannelWidth() => null;
    protected virtual int? GetConfigSamples() => null;
    protected virtual int? GetConfigBeatSubdivisions() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_pixelsPerBeatSpecified)
        {
            var v = GetConfigPixelsPerBeat();
            if (v.HasValue) _pixelsPerBeat = v.Value;
        }

        if (!_channelWidthSpecified)
        {
            var v = GetConfigChannelWidth();
            if (v.HasValue) _channelWidth = v.Value;
        }

        if (!_samplesSpecified)
        {
            var v = GetConfigSamples();
            if (v.HasValue) _samples = v.Value;
        }

        if (!_beatSubdivisionsSpecified)
        {
            var v = GetConfigBeatSubdivisions();
            if (v.HasValue) _beatSubdivisions = v.Value;
        }
    }

    /// <summary>
    /// 解析输出目录（若未指定 --output 则使用输入文件旁的 render_output 子目录）
    /// </summary>
    public string ResolveOutputDir()
    {
        if (!string.IsNullOrWhiteSpace(Output)) return Output;

        if (!string.IsNullOrWhiteSpace(Input))
            return Path.Combine(Path.GetDirectoryName(Input) ?? ".", "render_output");

        return Path.Combine(Directory.GetCurrentDirectory(), "render_output");
    }
}

