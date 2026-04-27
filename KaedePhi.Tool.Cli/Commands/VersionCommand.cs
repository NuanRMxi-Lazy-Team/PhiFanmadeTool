using System.Reflection;
using KaedePhi.Tool.Cli.Infrastructure;

namespace KaedePhi.Tool.Cli.Commands;

// Description set via WithDescription(Strings.cli_cmd_version_desc) in Program.cs
public sealed class VersionCommand : AsyncCommand<VersionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings,
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