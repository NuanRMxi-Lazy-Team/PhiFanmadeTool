using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class RpeUnbindFatherCommand : AsyncCommand<RpeUnbindFatherCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
        [CommandOption("--classic")]
        [LocalizedDescription("cli_opt_classic_mode_desc")]
        public bool Classic { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();
        // 订阅日志
        RePhiEditHelper.OnDebug += s => writer.Info(s);
        RePhiEditHelper.OnError += s => writer.Error(s);
        RePhiEditHelper.OnInfo += s => writer.Info(s);
        RePhiEditHelper.OnWarning += s => writer.Warn(s);

        for (var i = 0; i < chart.JudgeLineList.Count; i++)
        {
            if (chart.JudgeLineList[i].Father != -1)
                if (settings.Classic)
                    chartCopy.JudgeLineList[i] = await RePhiEditHelper.FatherUnbindAsync(
                        i, chart.JudgeLineList, settings.Precision, settings.Tolerance);
                else
                    chartCopy.JudgeLineList[i] = await RePhiEditHelper.FatherUnbindPlusAsync(
                        i, chart.JudgeLineList, settings.Precision, settings.Tolerance);
        }

        // 取消订阅
        RePhiEditHelper.OnDebug -= s => writer.Info(s);
        RePhiEditHelper.OnError -= s => writer.Error(s);
        RePhiEditHelper.OnInfo -= s => writer.Info(s);
        RePhiEditHelper.OnWarning -= s => writer.Warn(s);

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
        {
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, settings.FormatOutput);
            }
            else
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(settings.FormatOutput),
                    cancellationToken);
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}