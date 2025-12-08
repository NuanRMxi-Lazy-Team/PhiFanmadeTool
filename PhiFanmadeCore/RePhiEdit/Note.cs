using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class Note
{
    /// <summary>
    /// 音符是否在判定线上方下落，true为上方，false为下方
    /// </summary>
    [JsonProperty("above")] [JsonConverter(typeof(BoolConverter))]
    public bool Above = true;

    /// <summary>
    /// 音符的不透明度
    /// </summary>
    [JsonProperty("alpha")] public int Alpha = 255;

    /// <summary>
    /// 音符的起始拍
    /// </summary>
    [JsonProperty("startTime")] public Beat StartBeat = new([0, 0, 1]); // 开始时间

    /// <summary>
    /// 音符的结束拍
    /// </summary>
    [JsonProperty("endTime")] public Beat EndBeat = new([1, 0, 1]); // 结束时间

    /// <summary>
    /// 音符是否为假音符
    /// </summary>
    [JsonProperty("isFake")] [JsonConverter(typeof(BoolConverter))]
    public bool IsFake = false;

    /// <summary>
    /// 音符相对于判定线的X坐标
    /// </summary>
    [JsonProperty("positionX")] public float PositionX = 0.0f; // X坐标

    /// <summary>
    /// 音符宽度倍率
    /// </summary>
    [JsonProperty("size")] public float Size = 1.0f; // 宽度倍率

    /// <summary>
    /// 音符判定宽度倍率
    /// </summary>
    [JsonProperty("judgeArea")] public float JudgeArea = 1.0f;

    /// <summary>
    /// 音符下落速度倍率
    /// </summary>
    [JsonProperty("speed")] public float SpeedMultiplier = 1.0f; // 速度倍率

    /// <summary>
    /// 音符类型
    /// </summary>
    [JsonProperty("type")] [JsonConverter(typeof(NoteTypeConverter))]
    public NoteType Type = NoteType.Tap; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）

    /// <summary>
    /// 音符可见时间，单位为秒
    /// </summary>
    [JsonProperty("visibleTime")] public float VisibleTime = 999999.0000f; // 可见时间（单位为秒）

    /// <summary>
    /// 音符相对于判定线的Y轴偏移
    /// </summary>
    [JsonProperty("yOffset")] public float YOffset = 0.0f; // Y偏移

    /// <summary>
    /// 音符颜色（RGB，顶点颜色乘法），此字段在Json中优先为tint，早期版本使用过color字段
    /// </summary>
    [JsonConverter(typeof(ColorConverter))]
    public byte[] Color = [255, 255, 255]; // 颜色（RGB）

    /// <summary>
    /// 打击特效颜色（RGB，顶点颜色乘法）
    /// </summary>
    [JsonProperty("tintHitEffects", NullValueHandling = NullValueHandling.Ignore)]
    public byte[]? HitFxColor = null;

    /// <summary>
    /// 音符打击音效相对路径
    /// </summary>
    [JsonProperty("hitsound", NullValueHandling = NullValueHandling.Ignore)]
    public string? HitSound = null; // 音效
}

public enum NoteType
{
    Tap = 1,
    Hold = 2,
    Flick = 3,
    Drag = 4
}