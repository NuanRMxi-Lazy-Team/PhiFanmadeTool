using System;
using System.Collections.Generic;
using System.Linq;

namespace PhiFanmade.OpenTool.Cli.Parsing;

/// <summary>
/// 通用选项解析工具集。负责从 argv 中提取选项值、标志位与重复参数。
/// 说明：仅做解析，不做校验与本地化文案输出。
/// </summary>
public static class OptionParser
{
    /// <summary>
    /// 获取形如 -i/--input/--输入 后接的单值参数。
    /// 若未找到返回 null。
    /// </summary>
    public static string? GetOption(string[] argv, params string[] names)
    {
        for (int i = 0; i < argv.Length; i++)
        {
            if (names.Any(n => string.Equals(argv[i], n, StringComparison.OrdinalIgnoreCase)))
            {
                if (i + 1 < argv.Length) return argv[i + 1];
            }
        }
        return null;
    }

    /// <summary>
    /// 判断是否存在某个开关（无参选项），如 --json/--verbose 等。
    /// </summary>
    public static bool HasFlag(string[] argv, params string[] names)
        => names.Any(n => argv.Any(a => string.Equals(a, n, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// 获取可以重复出现的形如 -v/--var 的值列表（取紧随其后的一个值）。
    /// </summary>
    public static string[] GetMany(string[] argv, params string[] names)
    {
        var list = new List<string>();
        for (int i = 0; i < argv.Length; i++)
        {
            if (names.Any(n => string.Equals(argv[i], n, StringComparison.OrdinalIgnoreCase)))
            {
                if (i + 1 < argv.Length)
                    list.Add(argv[i + 1]);
            }
        }
        return list.ToArray();
    }

    /// <summary>
    /// 将 KEY=VALUE 形式的片段转换为字典。非法片段被忽略。
    /// </summary>
    public static Dictionary<string, string> ParseVars(IEnumerable<string> parts)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in parts)
        {
            var idx = p.IndexOf('=');
            if (idx <= 0 || idx == p.Length - 1) continue;
            var k = p[..idx];
            var v = p[(idx + 1)..];
            dict[k] = v;
        }
        return dict;
    }
}
