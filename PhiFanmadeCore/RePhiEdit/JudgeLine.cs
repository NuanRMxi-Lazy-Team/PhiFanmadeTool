using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class JudgeLine
{
    public string Name = "PhiFanmadeCoreJudgeLine";
    public string Texture = "line.png"; // 判定线纹理路径
    [JsonProperty("anchor")] public float[] Anchor = [0.5f, 0.5f]; // 判定线纹理锚点
    [JsonProperty("eventLayers")] public EventLayer[] EventLayers = []; // 事件层
    [JsonProperty("father")] public int Father = -1; // 父级

    [JsonProperty("isCover")] [JsonConverter(typeof(BoolConverter))]
    public bool IsCover = true; // 是否遮罩

    [JsonProperty("notes")] public Note[] Notes = []; // note列表
    [JsonProperty("zOrder")] public int ZOrder; // Z轴顺序

    [JsonProperty("attachUI", NullValueHandling = NullValueHandling.Ignore)] [JsonConverter(typeof(AttachUiConverter))]
    public AttachUi? AttachUi; // 绑定UI名，当不绑定时为null

    [JsonProperty("isGif")] public bool IsGif; // 纹理是否为GIF

    /// <summary>
    /// 绑定组
    /// </summary>
    [JsonProperty("linkgroup")] public int LinkGroup = 0; // 绑定组

    /// <summary>
    /// 当前判定线相对于当前BPM的因子。判定线BPM = 谱面BPM / BpmFactor
    /// </summary>
    [JsonProperty("bpmfactor")] public float BpmFactor = 1.0f; // BPM因子

    [JsonProperty("rotateWithFather")] public bool RotateWithFather = false; // 是否随父级旋转

    [JsonProperty("posControl")] public PosControl[] PositionControl =
    [
        new()
        {
            Easing = new(1),
            Pos = 1.0f,
            X = 0.0f
        },
        new()
        {
            Easing = new(1),
            Pos = 1.0f,
            X = 9999999.0f
        }
    ];
}

public enum AttachUi
{
    Pause = 1,
    ComboNumber = 2,
    Combo = 3,
    Score = 4,
    Bar = 5,
    Name = 6,
    Level = 7
}