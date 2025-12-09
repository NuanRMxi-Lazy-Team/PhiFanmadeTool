using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    /// <summary>
    /// 谱面元数据
    /// </summary>
    public class Meta
    {
        /// <summary>
        /// RePhiEdit版本号
        /// </summary>
        [JsonProperty("RPEVersion")] public int RpeVersion = 150; // RPE版本

        /// <summary>
        /// 曲绘的相对路径
        /// </summary>
        [JsonProperty("background")] public string Background = "0.jpg"; // 曲绘

        /// <summary>
        /// 谱师
        /// </summary>
        [JsonProperty("charter")] public string Charter = "PhiFanmadeCore"; // 谱师

        /// <summary>
        /// 谱面音乐作者
        /// </summary>
        [JsonProperty("composer")] public string Composer = "Unknown"; // 曲师

        /// <summary>
        /// 谱面曲绘作者
        /// </summary>
        [JsonProperty("illustration")] public string Illustration = "Unknown"; // 曲绘画师

        /// <summary>
        /// 谱面难度
        /// </summary>
        [JsonProperty("level")] public string Level = "NR  Lv.17"; // 难度

        /// <summary>
        /// 谱面名称
        /// </summary>
        [JsonProperty("name")] public string Name = "PhiFanmadeCore by NuanR_Star Ciallo Team"; // 曲名

        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        [JsonProperty("offset")] public int Offset = 0; // 音乐偏移

        /// <summary>
        /// 音乐的相对路径
        /// </summary>
        [JsonProperty("song")] public string Song = "0.wav"; // 音乐

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
    }
}