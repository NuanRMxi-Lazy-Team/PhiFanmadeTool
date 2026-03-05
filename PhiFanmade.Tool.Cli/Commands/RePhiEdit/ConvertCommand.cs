using PhiFanmade.Tool.Localization;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

public sealed class RpeConvertCommand : Command<BaseSettings>
{
    protected override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        settings.CreateWriter().Warn(Strings.cli_warn_rpe_convert);
        return 2;
    }
}