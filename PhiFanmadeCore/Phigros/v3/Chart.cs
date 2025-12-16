using Newtonsoft.Json;
// STJ 特性使用完全限定名

namespace PhiFanmade.Core.Phigros.v3
{
    public static partial class PhigrosV3
    {
        public class Chart
        {
            /// <summary>
            /// 格式版本号
            /// </summary>
            [JsonProperty("formatVersion")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("formatVersion")]
#endif
            public int FormatVersion = 3;

            /// <summary>
            /// 谱面偏移，单位为秒
            /// </summary>
            [JsonProperty("offset")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("offset")]
#endif
            public float Offset = 0;
        }
        
    }
}