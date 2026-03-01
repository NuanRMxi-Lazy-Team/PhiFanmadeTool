using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 命令处理器统一接口。
/// </summary>
public interface ICommandHandler
{
    Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc);
}
