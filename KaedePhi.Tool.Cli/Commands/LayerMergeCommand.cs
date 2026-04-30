using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings;
using KaedePhi.Tool.Layer.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class LayerMergeCommand : AsyncCommand<LayerMergeCommand.Settings>
{
    public sealed class Settings : OperationSettings;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings s, CancellationToken ct)
    {
        var c = s.AppConfig.LayerMergeConfig;
        s.Precision ??= c.Precision;
        s.Tolerance ??= c.Tolerance;
        s.Classic ??= c.ClassicMode;
        s.DisableCompress ??= c.DisableCompress;
        s.DryRun ??= c.DryRun;

        if (s.DisableCompress == true && s.Classic != true)
        {
            new ConsoleWriter().Error(Strings.cli_err_classic_disablsed);
            return 1;
        }

        var writer = new ConsoleWriter();
        var svc = new ChartService();
        var nrc = await svc.LoadKpcAsync(s.Input, s.Workspace, ct);
        if (nrc == null) { writer.Error(Strings.cli_err_unimplemented); return 1; }

        var nrcCopy = nrc.Clone();
        var processor = new KpcLayerProcessor();
        foreach (var line in nrcCopy.JudgeLineList)
        {
            if (line.EventLayers is not { Count: > 1 }) continue;
            line.EventLayers =
            [
                s.Classic == true
                    ? processor.LayerMerge(line.EventLayers, s.Precision ?? 64d)
                    : processor.LayerMergePlus(line.EventLayers, s.Precision ?? 64d, s.Tolerance ?? 5d)
            ];
        }

        var output = await svc.SaveAsRpeAsync(nrcCopy, svc.ResolveOutputPath(s.Input, s.Output, s.Workspace), s.DryRun ?? false, ct);
        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}
