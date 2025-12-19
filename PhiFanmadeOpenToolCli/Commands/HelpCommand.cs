using System.Reflection;
using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Localization;

namespace PhiFanmade.OpenTool.Cli.Commands;

public class HelpCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Info($"{loc["cli.msg.help"]}");
        return Task.FromResult(0);
    }
}