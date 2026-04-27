namespace KaedePhi.Tool.Cli.Model;

/// <summary>
/// 事件拟合命令默认配置
/// </summary>
public class FitConfig
{
    /// <summary>
    /// 拟合容差
    /// </summary>
    public double Tolerance { get; set; } = 0.5d;

    /// <summary>
    /// 是否为干运行（仅计算不写入）
    /// </summary>
    public bool DryRun { get; set; } = false;
}