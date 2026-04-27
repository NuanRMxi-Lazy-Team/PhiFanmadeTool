using Spectre.Console;

namespace KaedePhi.Tool.Cli.Infrastructure;

/// <summary>
/// 控制台输出封装。
/// 说明（i18n/l10n）：此类仅负责输出，不持有具体文案；
/// 文案应由调用方通过本地化后传入。
/// </summary>
public sealed class ConsoleWriter
{
    /// <summary>
    /// 信息级输出。
    /// </summary>
    public void Info(string message)
    {
        AnsiConsole.MarkupLine($"[green]{Escape(message)}[/]");
    }

    /// <summary>
    /// 警告级输出。
    /// </summary>
    public void Warn(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{Escape(message)}[/]");
    }

    /// <summary>
    /// 错误级输出。
    /// </summary>
    public void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Escape(message)}[/]");
    }

    private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");
}
