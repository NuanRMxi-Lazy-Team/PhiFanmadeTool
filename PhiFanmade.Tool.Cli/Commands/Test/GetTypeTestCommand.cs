using System.ComponentModel;
using PhiFanmade.Tool.Common;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.Test;

public class GetTypeTestCommand : AsyncCommand<GetTypeTestCommand.Settings>
{
    public class Settings : BaseSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("需要推算的文件")]
        public string? Input { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var input = settings.Input;
        var inputText = input is null ? "" : await File.ReadAllTextAsync(input, cancellationToken);
        var type = ChartGetType.GetType(inputText);
        writer.Info($"Type: {type}");
        return 0;
    }
}