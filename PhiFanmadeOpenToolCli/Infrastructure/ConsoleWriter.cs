using System.Text.Json;
using Spectre.Console;

namespace PhiFanmade.OpenTool.Cli.Infrastructure;

/// <summary>
/// 控制台输出封装。支持彩色文本与 JSON 两种输出形式。
/// 说明（i18n/l10n）：此类仅负责输出，不持有具体文案；
/// 文案应由调用方通过本地化后传入。
/// </summary>
public sealed class ConsoleWriter
{
    /// <summary>
    /// 是否以 JSON 行输出。为 true 时，将输出 {level, message} 对象。
    /// </summary>
    public bool Json { get; set; }

    /// <summary>
    /// 信息级输出。
    /// </summary>
    public void Info(string message)
    {
        if (Json)
        {
            System.Console.WriteLine(JsonSerializer.Serialize(new { level = "info", message }));
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]{Escape(message)}[/]");
        }
    }

    /// <summary>
    /// 警告级输出。
    /// </summary>
    public void Warn(string message)
    {
        if (Json)
        {
            System.Console.WriteLine(JsonSerializer.Serialize(new { level = "warn", message }));
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]{Escape(message)}[/]");
        }
    }

    /// <summary>
    /// 错误级输出。
    /// </summary>
    public void Error(string message)
    {
        if (Json)
        {
            System.Console.WriteLine(JsonSerializer.Serialize(new { level = "error", message }));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{Escape(message)}[/]");
        }
    }

    private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");
}
