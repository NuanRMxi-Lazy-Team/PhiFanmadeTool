namespace PhiFanmade.OpenTool.Cli.Localization;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
public static class SystemLanguage
{
    public static string GetPreferredLanguageTag()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsPreferredLanguageTag();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacPreferredLanguageTag()
                   ?? GetUnixLocaleLanguageTag(); // 兜底

        // Linux / other Unix
        return GetUnixLocaleLanguageTag();
    }

    public static CultureInfo GetPreferredCultureInfo()
    {
        var tag = GetPreferredLanguageTag();

        // tag 可能是 "zh_CN" / "zh-CN" / "en_US.UTF-8"
        tag = NormalizeToBcp47(tag);

        try
        {
            return CultureInfo.GetCultureInfo(tag);
        }
        catch (CultureNotFoundException)
        {
            // 兜底：只取语言部分，如 "zh-CN" -> "zh"
            var lang = tag.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                try { return CultureInfo.GetCultureInfo(lang); } catch { /* ignore */ }
            }
            return CultureInfo.InvariantCulture;
        }
    }

    // ---------------- Windows ----------------

    public static string GetWindowsPreferredLanguageTag()
    {
        const uint MUI_LANGUAGE_NAME = 0x8;

        uint numLanguages = 0;
        uint bufferSize = 0;

        // 第一次：拿所需字符数（包含末尾双 \0）
        if (!GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, out numLanguages, null, ref bufferSize) &&
            Marshal.GetLastWin32Error() != 122 /* ERROR_INSUFFICIENT_BUFFER */ &&
            bufferSize == 0)
        {
            return GetFallbackFromUserDefaultUILanguage();
        }

        if (bufferSize == 0)
            return GetFallbackFromUserDefaultUILanguage();

        var sb = new StringBuilder((int)bufferSize);

        if (!GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, out numLanguages, sb, ref bufferSize))
            return GetFallbackFromUserDefaultUILanguage();

        // 注意：这是 multi-string（用 '\0' 分隔，末尾 "\0\0"）
        // StringBuilder.ToString() 可能包含 '\0'，Split 才能拿到第一项
        var multi = sb.ToString();
        var first = multi.Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return string.IsNullOrWhiteSpace(first)
            ? GetFallbackFromUserDefaultUILanguage()
            : first;
    }

    private static string GetFallbackFromUserDefaultUILanguage()
    {
        // 返回 LANGID，需要转成 locale name
        ushort langId = GetUserDefaultUILanguage();

        // LANGID -> LCID (SORT_DEFAULT=0)
        uint lcid = MakeLcid(langId, 0);

        var sb = new StringBuilder(85); // LOCALE_NAME_MAX_LENGTH=85
        int len = LCIDToLocaleName(lcid, sb, sb.Capacity, 0);

        if (len > 0)
            return sb.ToString();

        // 最终兜底
        return "en-US";
    }

    private static uint MakeLcid(uint langId, uint sortId) => (sortId << 16) | langId;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetUserPreferredUILanguages(
        uint dwFlags,
        out uint pulNumLanguages,
        StringBuilder? pwszLanguagesBuffer,
        ref uint pcchLanguagesBuffer);

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int LCIDToLocaleName(
        uint Locale,
        StringBuilder lpName,
        int cchName,
        uint dwFlags);

    /// <summary>
    /// Retrieves the preferred language tag for macOS systems.
    /// This utilizes the `defaults` command to read the AppleLanguages user preference.
    /// The method returns the first entry in the language preference list,
    /// typically representing the primary preferred language of the user.
    /// If no language preference can be determined, the method returns null.
    /// </summary>
    /// <returns>
    /// A string representing the user's preferred language tag on macOS,
    /// or null if it cannot be determined.
    /// </returns>
    private static string? GetMacPreferredLanguageTag()
    {
        // 读取 defaults：AppleLanguages 是数组，第一项通常是用户首选语言（如 "zh-Hans-CN"）
        // 注意：沙盒/权限/环境不同可能失败，所以要兜底
        var output = TryRun("defaults", "read -g AppleLanguages");
        if (string.IsNullOrWhiteSpace(output)) return null;

        // 输出类似：
        // (
        //     "zh-Hans-CN",
        //     "en-CN"
        // )
        var firstQuoted = output.Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("\"", StringComparison.Ordinal) && l.EndsWith("\",", StringComparison.Ordinal)
                              || l.StartsWith("\"", StringComparison.Ordinal) && l.EndsWith("\"", StringComparison.Ordinal));

        if (firstQuoted == null) return null;

        var tag = firstQuoted.Trim().TrimEnd(',').Trim('"').Trim();
        return string.IsNullOrWhiteSpace(tag) ? null : tag;
    }

    // ---------------- Linux/Unix ----------------

    private static string GetUnixLocaleLanguageTag()
    {
        // 优先级：LC_ALL > LC_MESSAGES > LANG
        var lcAll = Environment.GetEnvironmentVariable("LC_ALL");
        var lcMsg = Environment.GetEnvironmentVariable("LC_MESSAGES");
        var lang  = Environment.GetEnvironmentVariable("LANG");

        var raw = FirstNonEmpty(lcAll, lcMsg, lang);
        raw = NormalizeToBcp47(raw);

        // 如果环境变量没有，尝试常见配置文件
        raw ??= TryReadLocaleFromEtc();

        // 最后兜底
        raw ??= "en-US";

        return raw;
    }

    private static string? TryReadLocaleFromEtc()
    {
        // 常见：/etc/locale.conf 里 LANG=xx_YY.UTF-8
        // 也可能：/etc/default/locale
        var paths = new[]
        {
            "/etc/locale.conf",
            "/etc/default/locale"
        };

        foreach (var p in paths)
        {
            try
            {
                if (!File.Exists(p)) continue;
                var lines = File.ReadAllLines(p);
                var langLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("LANG=", StringComparison.Ordinal));
                if (langLine == null) continue;
                var value = langLine.Split('=', 2).ElementAtOrDefault(1)?.Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch
            {
                // ignore and continue
            }
        }

        return null;
    }

    // ---------------- helpers ----------------

    private static string NormalizeToBcp47(string raw)
    {
        raw = raw.Trim();

        // 去掉编码：en_US.UTF-8 -> en_US
        var dot = raw.IndexOf('.');
        if (dot >= 0) raw = raw[..dot];

        // 去掉变体：de_DE@euro -> de_DE
        var at = raw.IndexOf('@');
        if (at >= 0) raw = raw[..at];

        // 下划线转横杠：zh_CN -> zh-CN
        raw = raw.Replace('_', '-');

        // 处理 "C" / "POSIX"
        if (string.Equals(raw, "C", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "POSIX", StringComparison.OrdinalIgnoreCase))
            return "en-US";

        return raw;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? TryRun(string fileName, string arguments)
    {
        try
        {
            using var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            if (!p.Start()) return null;

            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(1500);
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }
}