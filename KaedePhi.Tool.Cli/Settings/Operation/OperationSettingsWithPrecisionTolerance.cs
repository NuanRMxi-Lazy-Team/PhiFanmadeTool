using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.KaedePhi.Converters;
using Chart = KaedePhi.Core.Kpc.Chart;

namespace KaedePhi.Tool.Cli.Settings.Operation;

/// <summary>
/// 支持精度/容差调整的操作设置（用于 Cut/LayerMerge/Unbind 命令）
/// </summary>
public abstract class OperationSettingsWithPrecisionTolerance : OperationSettingsBase
{
    private double? _precision;
    private bool _precisionSpecified;
    private double? _tolerance;
    private bool _toleranceSpecified;
    private bool? _dryRun;
    private bool _dryRunSpecified;

    [CommandOption("-p|--precision <N>")]
    [LocalizedDescription("cli_opt_precision_desc")]
    public double Precision
    {
        get => _precision ?? 64d;
        set
        {
            _precisionSpecified = true;
            _precision = value;
        }
    }

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

    protected virtual double? GetConfigPrecisionDefault() => null;
    protected virtual double? GetConfigToleranceDefault() => null;
    protected virtual bool? GetConfigDryRunDefault() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_precisionSpecified)
        {
            var precision = GetConfigPrecisionDefault();
            if (precision.HasValue) _precision = precision.Value;
        }

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

    /// <summary>将 NRC 中间类型导出为目标谱面类型，并按当前设置写入。</summary>
    public override async Task<string?> SaveFromNrcAsync(Chart chart,
        CancellationToken cancellationToken = default)
    {
        var output = ResolveOutputPath();

        if (DryRun) return output;

        var rpeChart = KpcToRpe.Convert(chart);
        var json = await rpeChart.ExportToJsonAsync(false);
        await File.WriteAllTextAsync(output, json, cancellationToken);

        return output;
    }
}