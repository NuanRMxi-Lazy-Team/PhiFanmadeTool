using Newtonsoft.Json;

namespace PhiFanmade.Core.Phigros.v3;

public class Chart
{
    /// <summary>
    /// 格式版本号
    /// </summary>
    [JsonProperty("formatVersion")]
    public int FormatVersion = 3;
    /// <summary>
    /// 谱面偏移，单位为秒
    /// </summary>
    [JsonProperty("offset")]
    public float Offset = 0;
}