using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi.Converters;
using KaedePhi.Tool.KaedePhi.Converters.Model;
using Chart = KaedePhi.Core.Kpc.Chart;
using KpcToPe = KaedePhi.Tool.KaedePhi.Converters.KpcToPe;

namespace KaedePhi.Tool.Cli.Settings.Operation;

/// <summary>
/// 支持格式化/流式输出和转换目标选项的操作设置（用于 Convert 命令）
/// </summary>
public abstract class OperationSettingsWithFormatting : OperationSettingsBase
{
    private bool? _streamOutput;
    private bool _streamOutputSpecified;
    private bool? _formatOutput;
    private bool _formatOutputSpecified;
    private ChartType? _targetType;
    private bool _targetTypeSpecified;
    private bool? _dryRun;
    private bool _dryRunSpecified;

    [CommandOption("--stream")]
    [LocalizedDescription("cli_opt_stream_output_desc")]
    public bool StreamOutput
    {
        get => _streamOutput ?? false;
        set
        {
            _streamOutputSpecified = true;
            _streamOutput = value;
        }
    }

    [CommandOption("--format")]
    [LocalizedDescription("cli_opt_format_desc")]
    public bool FormatOutput
    {
        get => _formatOutput ?? false;
        set
        {
            _formatOutputSpecified = true;
            _formatOutput = value;
        }
    }

    [CommandOption("--target <TYPE>")]
    [LocalizedDescription("convert_command_opt_target")]
    public ChartType TargetType
    {
        get => _targetType ?? ChartType.RePhiEdit;
        set
        {
            _targetTypeSpecified = true;
            _targetType = value;
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

    protected virtual bool? GetConfigStreamOutputDefault() => null;
    protected virtual bool? GetConfigFormatOutputDefault() => null;
    protected virtual ChartType? GetConfigTargetTypeDefault() => null;
    protected virtual bool? GetConfigDryRunDefault() => null;

    public override void ApplyConfigDefaults()
    {
        base.ApplyConfigDefaults();

        if (!_streamOutputSpecified)
        {
            var stream = GetConfigStreamOutputDefault();
            if (stream.HasValue) _streamOutput = stream.Value;
        }

        if (!_formatOutputSpecified)
        {
            var format = GetConfigFormatOutputDefault();
            if (format.HasValue) _formatOutput = format.Value;
        }

        if (!_targetTypeSpecified)
        {
            var targetType = GetConfigTargetTypeDefault();
            if (targetType.HasValue) _targetType = targetType.Value;
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

        switch (TargetType)
        {
            case ChartType.RePhiEdit:
            {
                var rpeChart = KpcToRpe.Convert(chart);
                if (DryRun) return output;
                if (StreamOutput)
                {
                    await using var stream = new FileStream(output, FileMode.Create);
                    await rpeChart.ExportToJsonStreamAsync(stream, FormatOutput);
                }
                else
                {
                    var json = await rpeChart.ExportToJsonAsync(FormatOutput);
                    await File.WriteAllTextAsync(output, json, cancellationToken);
                }
            }
                break;
            case ChartType.PhiEdit:
            {
                var converter = new KpcToPe(new KaedePhiToPhiEditOptions());
                var peChart = converter.Convert(chart);
                if (DryRun) return output;
                if (StreamOutput)
                {
                    await using var stream = new FileStream(output, FileMode.Create);
                    await peChart.ExportToStreamAsync(stream);
                }
                else
                {
                    var pec = await peChart.ExportAsync();
                    await File.WriteAllTextAsync(output, pec, cancellationToken);
                }
            }
                break;
            default:
                return null;
        }

        return output;
    }
}