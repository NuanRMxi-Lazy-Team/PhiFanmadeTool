namespace KaedePhi.Tool.Cli.Model;

/// <summary>
/// 适用于YamlDotNet的默认配置文件
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 0 = 关闭日志, 1 = Debug日志, 2 = Info日志, 3 = Warning日志, 4 = Error日志
    /// </summary>
    public uint LogLevel { get; set; } = 2;

    /// <summary>
    /// 拟合默认参数设置
    /// </summary>
    public FitConfig FitConfig { get; set; } = new();

    /// <summary>
    /// 转换默认参数设置
    /// </summary>
    public ConvertConfig ConvertConfig { get; set; } = new();

    /// <summary>
    /// 切割默认参数设置
    /// </summary>
    public CutConfig CutConfig { get; set; } = new();

    /// <summary>
    /// 层级合并默认参数设置
    /// </summary>
    public LayerMergeConfig LayerMergeConfig { get; set; } = new();

    /// <summary>
    /// 父线解绑默认参数设置
    /// </summary>
    public UnbindConfig UnbindConfig { get; set; } = new();

    /// <summary>
    /// 事件通道渲染默认参数设置
    /// </summary>
    public RenderConfig RenderConfig { get; set; } = new();
}