using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Cli.Model;

/// <summary>
/// 格式转换命令默认配置
/// </summary>
public class ConvertConfig
{
    /// <summary>
    /// 转换目标格式
    /// </summary>
    public ChartType TargetType { get; set; } = ChartType.RePhiEdit;

    /// <summary>
    /// 是否美化格式化输出 JSON（仅在输出为文件时生效）
    /// </summary>
    public bool FormatOutput { get; set; } = false;

    /// <summary>
    /// 是否流式输出到文件（大文件推荐）
    /// </summary>
    public bool StreamOutput { get; set; } = false;

    /// <summary>
    /// 是否为干运行（仅计算不写入）
    /// </summary>
    public bool DryRun { get; set; } = false;
}