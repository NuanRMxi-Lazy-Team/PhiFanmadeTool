using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using KaedePhi.Tool.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class UnbindFatherCommand : AsyncCommand<UnbindFatherCommand.Settings>
{
    public sealed class Settings : OperationSettings;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings s, CancellationToken ct)
    {
        var c = s.AppConfig.UnbindConfig;
        s.Precision ??= c.Precision;
        s.Tolerance ??= c.Tolerance;
        s.Classic ??= c.ClassicMode;
        s.DryRun ??= c.DryRun;

        var writer = new ConsoleWriter();
        var svc = new ChartService();
        var nrc = await svc.LoadKpcAsync(s.Input, s.Workspace, ct);
        if (nrc == null) { writer.Error(Strings.cli_err_unimplemented); return 1; }

        var nrcCopy = nrc.Clone();
        var unbinder = new KpcJudgeLineUnbinder();
        using var _ = KpcToolLog.Subscribe(info: writer.Info, warning: writer.Warn, error: writer.Error, debug: writer.Info);

        for (var i = 0; i < nrc.JudgeLineList.Count; i++)
        {
            if (nrc.JudgeLineList[i].Father != -1)
                nrcCopy.JudgeLineList[i] = s.Classic == true
                    ? unbinder.FatherUnbind(i, nrc.JudgeLineList, s.Precision ?? 64d)
                    : unbinder.FatherUnbindPlus(i, nrc.JudgeLineList, s.Precision ?? 64d, s.Tolerance ?? 5d);
        }

        var output = await svc.SaveAsRpeAsync(nrcCopy, svc.ResolveOutputPath(s.Input, s.Output, s.Workspace), s.DryRun ?? false, ct);
        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}
