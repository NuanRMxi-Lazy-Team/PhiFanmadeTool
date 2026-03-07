using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

/// <summary>
/// 合并层级命令
/// </summary>
public sealed class RpeLayerMergeCommand : AsyncCommand<RpeLayerMergeCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();

        foreach (var jl in chartCopy.JudgeLineList)
        {
            if (jl.EventLayers is not { Count: > 1 }) continue;
            jl.EventLayers = [RePhiEditHelper.LayerMerge(jl.EventLayers, settings.Precision, settings.Tolerance)];
        }

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, settings.FormatOutput);
            }
            else
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(settings.FormatOutput),
                    cancellationToken);

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}