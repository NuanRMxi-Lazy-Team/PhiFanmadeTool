using Newtonsoft.Json;
// STJ 特性使用完全限定名

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        /// <summary>
        /// 谱面元数据
        /// </summary>
        public class Meta
        {
            /// <summary>
            /// RePhiEdit版本号
            /// </summary>
            [JsonProperty("RPEVersion")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("RPEVersion")]
#endif
            public int RpeVersion = 170; // RPE版本

            /// <summary>
            /// 曲绘的相对路径
            /// </summary>
            [JsonProperty("background")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("background")]
#endif
            public string Background = "0.jpg"; // 曲绘

            /// <summary>
            /// 谱师
            /// </summary>
            [JsonProperty("charter")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("charter")]
#endif
            public string Charter = "PhiFanmadeCore"; // 谱师

            /// <summary>
            /// 谱面音乐作者
            /// </summary>
            [JsonProperty("composer")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("composer")]
#endif
            public string Composer = "Unknown"; // 曲师

            /// <summary>
            /// 谱面曲绘作者
            /// </summary>
            [JsonProperty("illustration")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("illustration")]
#endif
            public string Illustration = "Unknown"; // 曲绘画师

            /// <summary>
            /// 谱面难度
            /// </summary>
            [JsonProperty("level")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("level")]
#endif
            public string Level = "NR  Lv.17"; // 难度

            /// <summary>
            /// 谱面名称
            /// </summary>
            [JsonProperty("name")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("name")]
#endif
            public string Name = "PhiFanmadeCore by NuanR_Star Ciallo Team"; // 曲名

            /// <summary>
            /// 谱面偏移，单位为毫秒
            /// </summary>
            [JsonProperty("offset")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("offset")]
#endif
            public int Offset = 0; // 音乐偏移

            /// <summary>
            /// 音乐的相对路径
            /// </summary>
            [JsonProperty("song")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("song")]
#endif
            public string Song = "0.wav"; // 音乐

            public override string ToString()
            {
                return $"RPEVersion: {RpeVersion}\n" +
                       $"Background: {Background}\n" +
                       $"Charter: {Charter}\n" +
                       $"Composer: {Composer}\n" +
                       $"Illustration: {Illustration}\n" +
                       $"Level: {Level}\n" +
                       $"Name: {Name}\n" +
                       $"Offset: {Offset}\n" +
                       $"Song: {Song}\n";
            }
            public Meta Clone()
            {
                // 这个没必要自己实现，直接MemberwiseClone就行
                return (Meta)MemberwiseClone();
            }
        }
    }
}