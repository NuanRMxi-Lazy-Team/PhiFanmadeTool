using KaedePhi.Tool.Cli.Infrastructure;

namespace KaedePhi.Tool.Cli.Settings.Operation;

/// <summary>
/// 支持精度/容差和压缩/经典模式的操作设置（用于 LayerMerge/Unbind 命令）
/// </summary>
public abstract class OperationSettingsWithPrecisionToleranceAndModes : OperationSettingsWithPrecisionTolerance
{
    private bool? _classic;
    private bool _classicSpecified;
    private bool? _disableCompress;
    private bool _disableCompressSpecified;

    [CommandOption("--classic")]
    [LocalizedDescription("cli_opt_classic_mode_desc")]
    public bool Classic
    {
        get => _classic ?? false;
        set
        {
            _classicSpecified = true;
            _classic = value;
        }
    }

    [CommandOption("--no-compress")]
    [LocalizedDescription("cli_opt_compress_desc")]
    public bool DisableCompress
    {
        get => _disableCompress ?? false;
        set
        {
            _disableCompressSpecified = true;
            _disableCompress = value;
        }
    }

    protected virtual bool? GetConfigClassicModeDefault() => null;
    protected virtual bool? GetConfigDisableCompressDefault() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_classicSpecified)
        {
            var classic = GetConfigClassicModeDefault();
            if (classic.HasValue) _classic = classic.Value;
        }

        if (!_disableCompressSpecified)
        {
            var disableCompress = GetConfigDisableCompressDefault();
            if (disableCompress.HasValue) _disableCompress = disableCompress.Value;
        }
    }
}

