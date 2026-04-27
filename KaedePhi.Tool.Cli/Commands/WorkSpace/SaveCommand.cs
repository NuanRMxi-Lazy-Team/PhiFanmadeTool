using KaedePhi.Tool.Cli.Infrastructure;
using Spectre.Console;

namespace KaedePhi.Tool.Cli.Commands.WorkSpace;

public sealed class SaveCommand : AsyncCommand<SaveCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-o|--output <PATH>")]
        [LocalizedDescription("cli_opt_output_path_desc")]
        public string? Output { get; set; }

        [CommandOption("-w|--workspace <ID>")]
        [LocalizedDescription("cli_opt_workspace_default_desc")]
        public string Workspace { get; set; } = "default";

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Output))
                return ValidationResult.Error(Strings.cli_err_output_required);
            return base.Validate();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var ws = new WorkspaceService();
        await ws.SaveAsync(settings.Workspace, settings.Output!);
        writer.Info(string.Format(Strings.cli_msg_saved, settings.Workspace, settings.Output!));
        return 0;
    }
}