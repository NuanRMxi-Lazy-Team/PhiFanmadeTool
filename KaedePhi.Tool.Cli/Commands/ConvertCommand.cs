using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings.Operation;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class ConvertCommand : AsyncCommand<ConvertCommand.Settings>
{
    public sealed class Settings : OperationSettingsWithFormatting
    {
        protected override ChartType? GetConfigTargetTypeDefault() => AppConfig.ConvertConfig?.TargetType;
        protected override bool? GetConfigFormatOutputDefault() => AppConfig.ConvertConfig?.FormatOutput;
        protected override bool? GetConfigStreamOutputDefault() => AppConfig.ConvertConfig?.StreamOutput;
        protected override bool? GetConfigDryRunDefault() => AppConfig.ConvertConfig?.DryRun;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        settings.ApplyConfigDefaults();
        var writer = new ConsoleWriter();

        var nrc = await settings.LoadNrcChartAsync(cancellationToken);
        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }
        // 订阅NRC日志
        using var logSubscription = KpcToolLog.Subscribe(
            info: writer.Info,
            warning: writer.Warn,
            error: writer.Error,
            debug: writer.Info);

        var output = await settings.SaveFromNrcAsync(nrc, cancellationToken);
        if (output == null)
        {
            writer.Warn(Strings.cli_warn_rpe_convert);
            return 2;
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}

