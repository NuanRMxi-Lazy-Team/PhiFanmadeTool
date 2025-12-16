using System.Globalization;
using System.Text.Json;

namespace PhiFanmade.OpenTool.Cli.Localization;

public interface ILocalizer
{
    /// <summary>
    /// 实际使用的语言标识，如 zh-CN 或 en-US。
    /// </summary>
    string Language { get; }

    /// <summary>
    /// 通过键获取本地化文案；不存在则回退到键名本身。
    /// </summary>
    string this[string key] { get; }
}

/// <summary>
/// 从 Localization *.json 读取文案，按系统语言自动选择，失败回退 en-US，再回退键名。
/// </summary>
public sealed class Localizer : ILocalizer
{
    private readonly Dictionary<string, string> _map;
    public string Language { get; }

    private Localizer(string lang, Dictionary<string, string> map)
    {
        Language = lang;
        _map = map;
    }

    public static ILocalizer Create()
    {
        var lang = SystemLanguage.GetPreferredLanguageTag();
        var loc = TryLoad(lang) ?? TryLoad("en-US") ?? new Localizer(lang, new());
        return loc;
    }

    private static Localizer? TryLoad(string lang)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Localization");
            var file = Path.Combine(dir, lang + ".json");
            if (!File.Exists(file)) return null;
            var json = File.ReadAllText(file);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            return new Localizer(lang, dict);
        }
        catch
        {
            return null;
        }
    }

    public string this[string key]
        => _map.TryGetValue(key, out var v) ? v : key;
}
