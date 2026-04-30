using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings;
using KaedePhi.Tool.KaedePhi;
using KaedePhi.Tool.KaedePhi.Events;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class FitEventCommand : AsyncCommand<FitEventCommand.Settings>
{
    public sealed class Settings : OperationSettings;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings s, CancellationToken ct)
    {
        var c = s.AppConfig.FitConfig;
        s.Tolerance ??= c.Tolerance;
        s.DryRun ??= c.DryRun;

        var writer = new ConsoleWriter();
        var svc = new ChartService();
        var nrc = await svc.LoadKpcAsync(s.Input, s.Workspace, ct);
        if (nrc == null) { writer.Error(Strings.cli_err_unimplemented); return 1; }

        var nrcCopy = nrc.Clone();
        using var _ = KpcToolLog.Subscribe(info: writer.Info, warning: writer.Warn, error: writer.Error, debug: writer.Info);

        var degree = Math.Max(1, Environment.ProcessorCount);
        var tol = s.Tolerance ?? 0.5d;

        for (var i = 0; i < nrc.JudgeLineList.Count; i++)
        {
            for (var j = 0; j < nrc.JudgeLineList[i].EventLayers.Count; j++)
            {
                var el = nrc.JudgeLineList[i].EventLayers[j];
                if (el == null) continue;
                ct.ThrowIfCancellationRequested();

                var mx = Task.Run(() => KpcEventTools.EventListFit(el.MoveXEvents, tol, degree), ct);
                var my = Task.Run(() => KpcEventTools.EventListFit(el.MoveYEvents, tol, degree), ct);
                var al = Task.Run(() => KpcEventTools.EventListFit(el.AlphaEvents, tol, degree), ct);
                var ro = Task.Run(() => KpcEventTools.EventListFit(el.RotateEvents, tol, degree), ct);
                await Task.WhenAll(mx, my, al, ro);

                nrcCopy.JudgeLineList[i].EventLayers[j].MoveXEvents = mx.Result;
                nrcCopy.JudgeLineList[i].EventLayers[j].MoveYEvents = my.Result;
                nrcCopy.JudgeLineList[i].EventLayers[j].AlphaEvents = al.Result;
                nrcCopy.JudgeLineList[i].EventLayers[j].RotateEvents = ro.Result;
            }
        }

        var output = await svc.SaveAsRpeAsync(nrcCopy, svc.ResolveOutputPath(s.Input, s.Output, s.Workspace), s.DryRun ?? false, ct);
        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}
