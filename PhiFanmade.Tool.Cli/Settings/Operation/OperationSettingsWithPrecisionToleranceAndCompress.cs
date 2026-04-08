using Spectre.Console.Cli;
using PhiFanmade.Tool.Localization;

namespace PhiFanmade.Tool.Cli.Settings.Operation;

/// <summary>
/// 支持精度/容差和压缩禁用的操作设置（用于 Cut 命令）
/// </summary>
public abstract class OperationSettingsWithPrecisionToleranceAndCompress : OperationSettingsWithPrecisionTolerance
{
    private bool? _disableCompress;
    private bool _disableCompressSpecified;

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

    protected virtual bool? GetConfigDisableCompressDefault() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_disableCompressSpecified)
        {
            var disableCompress = GetConfigDisableCompressDefault();
            if (disableCompress.HasValue) _disableCompress = disableCompress.Value;
        }
    }
}

