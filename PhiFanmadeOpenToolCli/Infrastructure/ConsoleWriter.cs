using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace PhiFanmade.OpenTool.Cli.Infrastructure;

/// <summary>
/// JSON 序列化上下文，用于 AOT 和 trimming 支持。
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LogMessage))]
internal partial class ConsoleWriterJsonContext : JsonSerializerContext
{
}

/// <summary>
/// 日志消息模型。
/// </summary>
internal sealed class LogMessage
{
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

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
            Console.WriteLine(JsonSerializer.Serialize(
                new LogMessage { Level = "info", Message = message },
                ConsoleWriterJsonContext.Default.LogMessage));
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
            Console.WriteLine(JsonSerializer.Serialize(
                new LogMessage { Level = "warn", Message = message },
                ConsoleWriterJsonContext.Default.LogMessage));
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
            Console.WriteLine(JsonSerializer.Serialize(
                new LogMessage { Level = "error", Message = message },
                ConsoleWriterJsonContext.Default.LogMessage));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{Escape(message)}[/]");
        }
    }

    private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");
}
