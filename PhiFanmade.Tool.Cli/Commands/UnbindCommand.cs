using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Cli.Settings.Operation;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.PhiFanmadeNrc;
using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class UnbindFatherCommand : AsyncCommand<UnbindFatherCommand.Settings>
{
    public sealed class Settings : OperationSettingsWithPrecisionToleranceAndModes
    {
        protected override double? GetConfigPrecisionDefault() => AppConfig.UnbindConfig.Precision;
        protected override double? GetConfigToleranceDefault() => AppConfig.UnbindConfig.Tolerance;
        protected override bool? GetConfigClassicModeDefault() => AppConfig.UnbindConfig.ClassicMode;
        protected override bool? GetConfigDisableCompressDefault() => AppConfig.UnbindConfig.DisableCompress;
        protected override bool? GetConfigDryRunDefault() => AppConfig.UnbindConfig.DryRun;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
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
        using var logSubscription = NrcToolLog.Subscribe(
            info: writer.Info,
            warning: writer.Warn,
            error: writer.Error,
            debug: writer.Info);

        for (var i = 0; i < nrc.JudgeLineList.Count; i++)
        {
            if (nrc.JudgeLineList[i].Father != -1)
                if (settings.Classic)
                    nrcCopy.JudgeLineList[i] = await NrcJudgeLineTools.FatherUnbindAsync(
                        i, nrc.JudgeLineList, settings.Precision, settings.Tolerance, !settings.DisableCompress);
                else
                    nrcCopy.JudgeLineList[i] = await NrcJudgeLineTools.FatherUnbindPlusAsync(
                        i, nrc.JudgeLineList, settings.Precision, settings.Tolerance);
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

