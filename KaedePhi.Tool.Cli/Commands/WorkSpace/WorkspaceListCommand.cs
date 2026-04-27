using KaedePhi.Tool.Cli.Infrastructure;

namespace KaedePhi.Tool.Cli.Commands.WorkSpace;

public sealed class WorkspaceListCommand : Command<WorkspaceListCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings,CancellationToken cancellationToken)
    {
        var ws = new WorkspaceService();
        foreach (var id in ws.List())
            Console.WriteLine(id);
        return 0;
    }
}