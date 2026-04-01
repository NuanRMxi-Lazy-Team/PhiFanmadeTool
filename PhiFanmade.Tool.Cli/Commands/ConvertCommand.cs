using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Common;
using PhiFanmade.Tool.Localization;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

public sealed class ConvertCommand : AsyncCommand<ConvertCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
        [CommandOption("--target <TYPE>")] 
        [LocalizedDescription("convert_command_opt_target")]
        public ChartType TargetType { get; set; } = ChartType.RePhiEdit;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        writer.Info(settings.TargetType.ToString());

        var chartText = await settings.LoadChartAsync();
        // 推断谱面格式
        var chartType = Common.ChartGetType.GetType(chartText);
        NrcCore.Chart? nrc = null;
        if (chartType == Common.ChartType.RePhiEdit)
        {
            var chart = await RpeCore.Chart.LoadFromJsonAsync(chartText);
            nrc = PhiFanmade.Tool.RePhiEdit.Converters.RpeToNrc.Convert(chart);
        }
        else if (chartType == Common.ChartType.PhiEdit)
        {
            var chart = await PeCore.Chart.LoadAsync(chartText);
            nrc = PhiFanmade.Tool.PhiEdit.Converters.PeToNrc.Convert(chart);
        }

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_ukerr));
            return 1;
        }

        if (settings.TargetType == ChartType.RePhiEdit)
        {
            var rpeResult = NrcTool.Converters.NrcToRpe.Convert(nrc);
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
            writer.Warn(Strings.cli_warn_rpe_convert);
            return 2;
        }


        writer.Warn(Strings.cli_warn_rpe_convert);
        return 2;
    }
}