using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings.Operation;
using KaedePhi.Tool.KaedePhi.Layers;

namespace KaedePhi.Tool.Cli.Commands;

/// <summary>
/// 合并层级命令
/// </summary>
public sealed class LayerMergeCommand : AsyncCommand<LayerMergeCommand.Settings>
{
    public sealed class Settings : OperationSettingsWithPrecisionToleranceAndModes
    {
        protected override double? GetConfigPrecisionDefault() => AppConfig.LayerMergeConfig?.Precision;
        protected override double? GetConfigToleranceDefault() => AppConfig.LayerMergeConfig?.Tolerance;
        protected override bool? GetConfigClassicModeDefault() => AppConfig.LayerMergeConfig?.ClassicMode;
        protected override bool? GetConfigDisableCompressDefault() => AppConfig.LayerMergeConfig?.DisableCompress;
        protected override bool? GetConfigDryRunDefault() => AppConfig.LayerMergeConfig?.DryRun;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        settings.ApplyConfigDefaults();
        var writer = new ConsoleWriter();
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);

        if (settings is { DisableCompress: true, Classic: false })
        {
            writer.Error(string.Format(Strings.cli_err_classic_disablsed));
            return 1;
        }

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }

        var nrcCopy = nrc.Clone();
        foreach (var line in nrcCopy.JudgeLineList)
        {
            if (line.EventLayers is not { Count: > 1 }) continue;

            line.EventLayers =
            [
                settings.Classic
                    ? KpcLayerTools.LayerMerge(line.EventLayers, settings.Precision)
                    : KpcLayerTools.LayerMergePlus(line.EventLayers, settings.Precision, settings.Tolerance)
            ];
        }

        var output = await settings.SaveFromNrcAsync(nrcCopy, cancellationToken);
        if (output == null)
        {
            writer.Warn(Strings.cli_warn_rpe_convert);
            return 2;
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}
