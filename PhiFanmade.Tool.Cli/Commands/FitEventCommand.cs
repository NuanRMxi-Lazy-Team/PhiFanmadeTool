using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.PhiFanmadeNrc;
using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class FitEventCommand : AsyncCommand<FitEventCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chartText = await settings.LoadChartAsync();

        // 推断谱面格式
        var chartType = Common.ChartGetType.GetType(chartText);
        NrcCore.Chart? nrc = null;
        if (chartType == Common.ChartType.RePhiEdit)
        {
            var chart = await RpeCore.Chart.LoadFromJsonAsync(chartText);
            nrc = PhiFanmade.Tool.RePhiEdit.Converters.RpeToNrc.Convert(chart);
        }

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_ukerr));
            return 1;
        }

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
            var jdl = nrc.JudgeLineList[i];
            for (var index = 0; index < jdl.EventLayers.Count; index++)
            {
                var eventLayer = jdl.EventLayers[index];
                if (eventLayer == null) continue;
                nrcCopy.JudgeLineList[i].EventLayers[index].MoveXEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.MoveXEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].MoveYEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.MoveYEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].AlphaEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.AlphaEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].RotateEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.RotateEvents,
                    settings.Precision, settings.Tolerance);
            }
        }

        var rpeResult = NrcTool.Converters.NrcToRpe.Convert(nrcCopy);

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
}