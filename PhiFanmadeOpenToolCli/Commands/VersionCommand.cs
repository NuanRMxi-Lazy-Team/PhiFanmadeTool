using System.Reflection;
using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Localization;

namespace PhiFanmade.OpenTool.Cli.Commands;

public sealed class VersionCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        writer.Info($"{loc["cli.app.title"]} v{ver}");
        return Task.FromResult(0);
    }
}
