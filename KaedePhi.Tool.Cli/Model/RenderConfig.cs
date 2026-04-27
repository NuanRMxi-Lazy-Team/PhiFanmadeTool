namespace KaedePhi.Tool.Cli.Model;

/// <summary>
/// 事件通道渲染命令默认配置
/// </summary>
public class RenderConfig
{
    /// <summary>每拍对应像素高度（默认 100）</summary>
    public float PixelsPerBeat { get; set; } = 100f;

    /// <summary>每个通道的宽度（像素，默认 150）</summary>
    public int ChannelWidth { get; set; } = 150;

    /// <summary>每个事件的曲线采样点数（默认 64）</summary>
    public int SamplesPerEvent { get; set; } = 64;

    /// <summary>默认输出目录（为空时使用输入文件所在目录）</summary>
    public string OutputDir { get; set; } = "";

    /// <summary>每拍格线细分数（1=只绘节拍线，4=绘四分音符线，默认 2）</summary>
    public int BeatSubdivisions { get; set; } = 2;
}

