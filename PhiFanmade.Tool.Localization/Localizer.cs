using System.Globalization;
using System.Text.Json;
using System.Reflection;

namespace PhiFanmade.Tool.Localization;

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
    public static Action<string> OnError { get; set; } = msg => { };

    private static JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    private Localizer(string lang, Dictionary<string, string> map)
    {
        Language = lang;
        _map = map;
    }

    public static ILocalizer Create()
    {
        var lang = CultureInfo.CurrentCulture.Name;
        var loc = TryLoad(lang) ?? TryLoad("en-US") ?? new Localizer(lang, new Dictionary<string, string>());
        return loc;
    }

    private static Localizer? TryLoad(string lang)
    {
        try
        {
            // 读取内嵌的json文件
            var embFileName = $"PhiFanmade.Tool.Localization.{lang}.json";
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(embFileName);
            if (stream is null) return null;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options) ??
                       new Dictionary<string, string>();
            return new Localizer(lang, dict);
        }
        catch (Exception e)
        {
            OnError.Invoke("Failed to load localization for " + lang + ": " + e.Message);
            //Console.WriteLine("Failed to load localization for " + lang + ": " + e.Message);
            return null;
        }
    }

    public string this[string key]
        => _map.GetValueOrDefault(key, key);
}