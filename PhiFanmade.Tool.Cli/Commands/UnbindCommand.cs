using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.PhiFanmadeNrc;
using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;
using PhiFanmade.Tool.RePhiEdit;
using PhiFanmade.Tool.RePhiEdit.JudgeLines;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class UnbindFatherCommand : AsyncCommand<UnbindFatherCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
        [CommandOption("--classic")]
        [LocalizedDescription("cli_opt_classic_mode_desc")]
        public bool Classic { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chartText = await settings.LoadChartAsync();
        if (settings is { DisableCompress: true, Classic: false })
        {
            writer.Error(string.Format(Strings.cli_err_classic_disablsed));
            return 1;
        }

        // 推断谱面格式
        var chartType = PhiFanmade.Tool.Common.ChartGetType.GetType(chartText);
        if (chartType == PhiFanmade.Tool.Common.ChartType.RePhiEdit)
        {
            var chart = await CoreRpe.Chart.LoadFromJsonAsync(chartText);
            var nrc = PhiFanmade.Tool.RePhiEdit.Converters.RpeToNrc.Convert(chart);


            var nrcCopy = nrc.Clone();
            // 订阅日志
            var onDebug = writer.Info;
            var onError = writer.Error;
            var onInfo = writer.Info;
            var onWarning = writer.Warn;
            NrcToolLog.OnDebug += onDebug;
            NrcToolLog.OnError += onError;
            NrcToolLog.OnInfo += onInfo;
            NrcToolLog.OnWarning += onWarning;

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
            var rpeResult = PhiFanmade.Tool.PhiFanmadeNrc.Converters.NrcToRpe.Convert(nrcCopy);

            // 取消订阅
            NrcToolLog.OnDebug -= onDebug;
            NrcToolLog.OnError -= onError;
            NrcToolLog.OnInfo -= onInfo;
            NrcToolLog.OnWarning -= onWarning;

            var output = settings.ResolveOutputPath();
            if (!settings.DryRun)
            {
                if (settings.StreamOutput)
                {
                    await using var stream = new FileStream(output, FileMode.Create);
                    await rpeResult.ExportToJsonStreamAsync(stream, settings.FormatOutput);
                }
                else
                    await File.WriteAllTextAsync(output, await rpeResult.ExportToJsonAsync(settings.FormatOutput),
                        cancellationToken);
            }

            writer.Info(string.Format(Strings.cli_msg_written, output));
            return 0;
        }
        else
        {
            writer.Error(string.Format(Strings.cli_err_unknown, chartType));
            return 1;
        }
    }
}