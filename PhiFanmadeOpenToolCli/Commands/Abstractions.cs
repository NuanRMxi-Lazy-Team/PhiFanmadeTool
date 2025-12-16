using System.Threading.Tasks;
using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Localization;

namespace PhiFanmade.OpenTool.Cli.Commands;

/// <summary>
/// 命令处理器统一接口。
/// </summary>
public interface ICommandHandler
{
    Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc);
}
