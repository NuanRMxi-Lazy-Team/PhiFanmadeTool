using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Info
        {
            [JsonProperty("name")] public string Name = ""; // 曲名
            [JsonProperty("artist")] public string Artist = ""; // 曲师
            [JsonProperty("illustration")] public string Illustration = ""; // 插画
            [JsonProperty("level")] public string Level = ""; // 等级
            [JsonProperty("designer")] public string Designer = ""; // 谱师
        }
    }
}