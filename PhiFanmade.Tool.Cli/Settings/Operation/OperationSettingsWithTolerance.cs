using Spectre.Console.Cli;
using PhiFanmade.Tool.Localization;

namespace PhiFanmade.Tool.Cli.Settings.Operation;

/// <summary>
/// 支持仅容差调整的操作设置（用于 FitEvent 命令）
/// </summary>
public abstract class OperationSettingsWithTolerance : OperationSettingsBase
{
    private double? _tolerance;
    private bool _toleranceSpecified;
    private bool? _dryRun;
    private bool _dryRunSpecified;

    [CommandOption("-t|--tolerance <N>")]
    [LocalizedDescription("cli_opt_tolerance_desc")]
    public double Tolerance
    {
        get => _tolerance ?? 5d;
        set
        {
            _toleranceSpecified = true;
            _tolerance = value;
        }
    }

    [CommandOption("--dry-run")]
    [LocalizedDescription("cli_opt_dry_run_desc")]
    public bool DryRun
    {
        get => _dryRun ?? false;
        set
        {
            _dryRunSpecified = true;
            _dryRun = value;
        }
    }

    protected virtual double? GetConfigToleranceDefault() => null;
    protected virtual bool? GetConfigDryRunDefault() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_toleranceSpecified)
        {
            var tolerance = GetConfigToleranceDefault();
            if (tolerance.HasValue) _tolerance = tolerance.Value;
        }

        if (!_dryRunSpecified)
        {
            var dryRun = GetConfigDryRunDefault();
            if (dryRun.HasValue) _dryRun = dryRun.Value;
        }
    }
}

