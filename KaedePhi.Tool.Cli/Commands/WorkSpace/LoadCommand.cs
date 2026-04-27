using KaedePhi.Tool.Cli.Infrastructure;
using Spectre.Console;

namespace KaedePhi.Tool.Cli.Commands.WorkSpace;

// Description set via WithDescription(Strings.cli_cmd_load_desc) in Program.cs
public sealed class LoadCommand : AsyncCommand<LoadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [LocalizedDescription("cli_opt_input_phiedit_desc")]
        public string? Input { get; set; }

        [CommandOption("-w|--workspace <ID>")]
        [LocalizedDescription("cli_opt_workspace_default_desc")]
        public string Workspace { get; set; } = "default";

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Input))
                return ValidationResult.Error(Strings.cli_err_input_required);
            return base.Validate();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var ws = new WorkspaceService();
        await ws.LoadAsync(settings.Workspace, settings.Input!);
        writer.Info(string.Format(Strings.cli_msg_loaded, settings.Workspace));
        return 0;
    }
}