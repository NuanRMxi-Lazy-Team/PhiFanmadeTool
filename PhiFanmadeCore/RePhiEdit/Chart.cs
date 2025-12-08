using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class Chart
{
    /// <summary>
    /// 序列化为Json
    /// </summary>
    /// <returns>Json</returns>
    public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
    /// <summary>
    /// 从Json反序列化
    /// </summary>
    /// <param name="json">谱面Json数据</param>
    /// <returns>谱面对象</returns>
    /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
    public static Chart FromJson(string json) => JsonConvert.DeserializeObject<Chart>(json) ?? throw new InvalidOperationException("Failed to deserialize Chart from JSON.");
    public const float MaxX = 675f;
    public const float MinX = -675f;
    public const float MaxY = 450f;
    public const float MinY = -450f;
    /// <summary>
    /// BPM列表
    /// </summary>
    [JsonProperty("BPMList")] public Bpm[] BpmList = [];

    /// <summary>
    /// 元数据
    /// </summary>
    [JsonProperty("META")] public Meta Meta = new();

    /// <summary>
    /// 判定线列表
    /// </summary>
    [JsonProperty("judgeLineList")] public JudgeLine[] JudgeLineList = [];

    /// <summary>
    /// 制谱时长（秒）
    /// </summary>
    [JsonProperty("chartTime")] public double ChartTime = 0d;

    /// <summary>
    /// 判定线组
    /// </summary>
    [JsonProperty("judgeLineGroup")] public string[] JudgeLineGroup = ["Default"];

    /// <summary>
    /// 多线编辑判定线列表（以空格为分割，或使用x:y选中x~y所有判定线）
    /// </summary>
    [JsonProperty("multiLineString")] public string MultiLineString = "1";
    
    /// <summary>
    /// 多线编辑页面缩放比例
    /// </summary>
    [JsonProperty("multiScale")] public float MultiScale = 1.0f;

    /// <summary>
    /// XY事件是否一一对应
    /// </summary>
    [JsonProperty("xybind")] public bool XyBind = true;
}