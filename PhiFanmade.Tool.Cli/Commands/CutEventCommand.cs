using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 切割事件命令
/// </summary>
public sealed class CutEventCommand : AsyncCommand<CutEventCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
        [CommandOption("--no-compress")]
        [LocalizedDescription("cli_opt_compress_desc")]
        public bool DisableCompress { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }

        var nrcCopy = nrc.Clone();
        for (var i = 0; i < nrcCopy.JudgeLineList.Count; i++)
        {
            var line = nrcCopy.JudgeLineList[i];
            line.EventLayers = NrcLayerTools.CutLayerEvents(line.EventLayers, settings.Precision, settings.Tolerance,
                !settings.DisableCompress);
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
