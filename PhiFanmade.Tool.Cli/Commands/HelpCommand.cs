using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;

namespace PhiFanmade.Tool.Cli.Commands;

public class HelpCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Info($"{loc["cli.msg.help"]}");
        return Task.FromResult(0);
    }
}