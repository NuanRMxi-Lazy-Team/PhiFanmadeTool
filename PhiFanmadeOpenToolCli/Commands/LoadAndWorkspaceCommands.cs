using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Localization;
using PhiFanmade.OpenTool.Cli.Parsing;

namespace PhiFanmade.OpenTool.Cli.Commands;

public sealed class LoadCommand : ICommandHandler
{
    public async Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var input = OptionParser.GetOption(args, "-i", "--input", "--输入");
        var workspace = OptionParser.GetOption(args, "--workspace", "--工作区") ?? "default";
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException(loc["err.input.required"]);
        var ws = new WorkspaceService();
        await ws.LoadAsync(workspace, input);
        writer.Info(loc["cli.msg.loaded"].Replace("{workspace}", workspace));
        return 0;
    }
}

public sealed class SaveCommand : ICommandHandler
{
    public async Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var output = OptionParser.GetOption(args, "-o", "--output", "--输出");
        var workspace = OptionParser.GetOption(args, "--workspace", "--工作区") ?? "default";
        if (string.IsNullOrWhiteSpace(output))
            throw new ArgumentException(loc["err.output.required"]);
        var ws = new WorkspaceService();
        await ws.SaveAsync(workspace, output!);
        writer.Info(loc["cli.msg.saved"].Replace("{workspace}", workspace).Replace("{path}", output!));
        return 0;
    }
}

public sealed class WorkspaceListCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var ws = new WorkspaceService();
        foreach (var id in ws.List())
        {
            Console.WriteLine(id);
        }
        return Task.FromResult(0);
    }
}

public sealed class WorkspaceClearCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var ws = new WorkspaceService();
        ws.Clear();
        writer.Info(loc["cli.msg.cleared"]);
        return Task.FromResult(0);
    }
}
