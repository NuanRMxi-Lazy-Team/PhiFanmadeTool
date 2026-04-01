using System.ComponentModel;
using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Common;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.Test;

public class GetTypeTestCommand : AsyncCommand<GetTypeTestCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("需要推算的文件")]
        public string? Input { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
#if Debug
        var input = settings.Input;
        var inputText = input is null ? "" : await File.ReadAllTextAsync(input, cancellationToken);
        var type = ChartGetType.GetType(inputText);
        writer.Info($"Type: {type}");
#else
        writer.Warn("This command can only be executed on Debug builds.");
        await Task.CompletedTask;
#endif
        
        return 0;
    }
}