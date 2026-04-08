namespace PhiFanmade.Tool.Cli.Model;

/// <summary>
/// 父线解绑命令默认配置
/// </summary>
public class UnbindConfig
{
    /// <summary>
    /// 切割精度
    /// </summary>
    public double Precision { get; set; } = 64d;
    
    /// <summary>
    /// 合并后压缩拟合容差
    /// </summary>
    public double Tolerance { get; set; } = 2.5d;
    
    /// <summary>
    /// 合并后是否禁用压缩行为
    /// </summary>
    public bool DisableCompress { get; set; } = false;
    
    /// <summary>
    /// 是否使用经典模式
    /// </summary>
    public bool ClassicMode { get; set; } = false;

    /// <summary>
    /// 是否为干运行（仅计算不写入）
    /// </summary>
    public bool DryRun { get; set; } = false;
}