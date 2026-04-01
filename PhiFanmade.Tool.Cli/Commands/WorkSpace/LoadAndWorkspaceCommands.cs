using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.WorkSpace;

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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var ws = new WorkspaceService();
        await ws.LoadAsync(settings.Workspace, settings.Input!);
        writer.Info(string.Format(Strings.cli_msg_loaded, settings.Workspace));
        return 0;
    }
}

// Description set via WithDescription(Strings.cli_cmd_save_desc) in Program.cs
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var ws = new WorkspaceService();
        await ws.SaveAsync(settings.Workspace, settings.Output!);
        writer.Info(string.Format(Strings.cli_msg_saved, settings.Workspace, settings.Output!));
        return 0;
    }
}

// ─── Workspace List ───────────────────────────────────────────────────────────

// Description set via WithDescription(Strings.cli_cmd_workspace_list_desc) in Program.cs
public sealed class WorkspaceListCommand : Command<CommandSettings>
{
    public override int Execute(CommandContext context, CommandSettings settings,CancellationToken cancellationToken)
    {
        var ws = new WorkspaceService();
        foreach (var id in ws.List())
            Console.WriteLine(id);
        return 0;
    }
}

// ─── Workspace Clear ──────────────────────────────────────────────────────────

// Description set via WithDescription(Strings.cli_cmd_workspace_clear_desc) in Program.cs
public sealed class WorkspaceClearCommand : Command<WorkspaceClearCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--id <ID>")]
        [LocalizedDescription("cli_opt_workspace_clear_id_desc")]
        public string? Id { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
        var ws = new WorkspaceService();
        ws.Clear(settings.Id);
        writer.Info(Strings.cli_msg_cleared);
        return 0;
    }
}
