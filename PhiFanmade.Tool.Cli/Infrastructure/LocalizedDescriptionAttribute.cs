using System.ComponentModel;
using System.Globalization;
using PhiFanmade.Tool.Localization;

namespace PhiFanmade.Tool.Cli.Infrastructure;

/// <summary>
/// <see cref="DescriptionAttribute"/> 的本地化变体。
/// 构造函数接受 .resx 资源键（编译时常量），
/// 在 Spectre.Console 读取 <see cref="Description"/> 属性时从资源管理器动态查找，
/// 从而支持命令选项描述的完整本地化。
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
{
    private readonly string _resourceKey;

    /// <param name="resourceKey"><see cref="Strings"/> (.resx) 中对应的资源键。</param>
    public LocalizedDescriptionAttribute(string resourceKey) : base(resourceKey)
    {
        _resourceKey = resourceKey;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 在运行时从资源管理器解析，遵从 <see cref="CultureInfo.CurrentUICulture"/>。
    /// 若找不到对应键，则回退为键名本身。
    /// </remarks>
    public override string Description =>
        Strings.ResourceManager.GetString(_resourceKey, CultureInfo.CurrentUICulture)
        ?? CliLocalizationString.ResourceManager.GetString(_resourceKey, CultureInfo.CurrentCulture) 
        ?? _resourceKey;
}