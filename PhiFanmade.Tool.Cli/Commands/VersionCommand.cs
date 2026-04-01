using System.Reflection;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

// Description set via WithDescription(Strings.cli_cmd_version_desc) in Program.cs
public sealed class VersionCommand : AsyncCommand<CommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, CommandSettings settings,
        CancellationToken cancellationToken)
    {
#if PreRelease || Release
        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
#else
        var ver = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
#endif
        new ConsoleWriter().Info($"{CliLocalizationString.app_title} v{ver}");
        return Task.FromResult(0);
    }
}