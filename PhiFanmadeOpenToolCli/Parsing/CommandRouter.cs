using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Cli.Commands;
using PhiFanmade.OpenTool.Cli.Localization;

namespace PhiFanmade.OpenTool.Cli.Parsing;

/// <summary>
/// 命令解析与分派。
/// - 解析单段/两段式命令（如 "version"、"rpe layer-merge"）。
/// - 支持多语言别名映射到标准命令键。
/// - 将执行委托给对应命令处理器。
/// </summary>
public sealed class CommandRouter
{
    // 标准命令键 -> 别名列表（含多语言）。不区分大小写。
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["version"] = new []{"version", "ver", "版本"},
        ["load"] = new []{"load", "载入"},
        ["save"] = new []{"save", "保存"},
        ["workspace.list"] = new []{"workspace.list", "workspace ls", "workspace list", "工作区 列表", "工作区 list"},
        ["workspace.clear"] = new []{"workspace.clear", "workspace clear", "工作区 清理"},
        ["rpe.unbind-father"] = new []{"rpe.unbind-father","unbind", "解绑父级"},
        ["rpe.layer-merge"] = new []{"rpe.layer-merge", "合并所有事件层级"},
        ["pe.convert"] = new []{"pe.convert", "转换为PE谱面"},
    };

    private readonly ConsoleWriter _writer = new();
    private readonly ILocalizer _loc = Localizer.Create();

    public async Task<int> RunAsync(string[] args)
    {
        // 先处理全局输出格式
        _writer.Json = OptionParser.HasFlag(args, "--json");

        var key = ResolveKey(args);
        if (key is null)
        {
            _writer.Error(_loc["err.unknown"]);
            return 1;
        }

        try
        {
            var handler = CreateHandler(key);
            return await handler.ExecuteAsync(args, _writer, _loc);
        }
        catch (Exception ex)
        {
            _writer.Error(ex.Message);
            return 1;
        }
    }

    private static string? ResolveKey(string[] argv)
    {
        if (argv.Length == 0) return "version";
        var firstTwo = argv.Length >= 2 ? $"{argv[0]}.{argv[1]}" : argv[0];
        foreach (var kv in Aliases)
        {
            if (kv.Value.Any(a => string.Equals(a, firstTwo, StringComparison.OrdinalIgnoreCase)))
                return kv.Key;
            if (kv.Value.Any(a => string.Equals(a, argv[0], StringComparison.OrdinalIgnoreCase)))
                return kv.Key;
        }
        return null;
    }

    private static ICommandHandler CreateHandler(string key) => key switch
    {
        "version" => new VersionCommand(),
        "load" => new LoadCommand(),
        "save" => new SaveCommand(),
        "workspace.list" => new WorkspaceListCommand(),
        "workspace.clear" => new WorkspaceClearCommand(),
        "rpe.unbind-father" => new RpeUnbindFatherCommand(),
        "rpe.layer-merge" => new RpeLayerMergeCommand(),
        "pe.convert" => new PeConvertCommand(),
        _ => new UnknownCommand()
    };
}
