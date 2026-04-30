using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class ConvertCommand : AsyncCommand<ConvertCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
        [CommandOption("--target <TYPE>")]
        [LocalizedDescription("convert_command_opt_target")]
        public ChartType? TargetType { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings s, CancellationToken ct)
    {
        var c = s.AppConfig.ConvertConfig;
        s.TargetType ??= c.TargetType;
        s.StreamOutput ??= c.StreamOutput;
        s.FormatOutput ??= c.FormatOutput;
        s.DryRun ??= c.DryRun;

        var writer = new ConsoleWriter();
        var svc = new ChartService();

        var kpc = await svc.LoadKpcAsync(s.Input, s.Workspace, ct);
        if (kpc == null) { writer.Error(Strings.cli_err_unimplemented); return 1; }

        using var _ = KpcToolLog.Subscribe(info: writer.Info, warning: writer.Warn, error: writer.Error, debug: writer.Info);

        var output = svc.ResolveOutputPath(s.Input, s.Output, s.Workspace);
        var result = await svc.SaveAsAsync(kpc, output, s.TargetType ?? ChartType.RePhiEdit,
            s.StreamOutput ?? false, s.FormatOutput ?? false, s.DryRun ?? false, ct);

        if (result == null) { writer.Warn(Strings.cli_warn_rpe_convert); return 2; }
        writer.Info(string.Format(Strings.cli_msg_written, result));
        return 0;
    }
}
