using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public class Chart
    {
        /// <summary>
        /// 序列化为Json
        /// </summary>
        /// <returns>Json</returns>
        [Obsolete("请使用 ExportToJson 方法代替")]
        public string ToJson() => ExportToJson(true);

        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        [Obsolete("请使用 LoadFromJson 方法代替")]
        public static Chart FromJson(string json) => LoadFromJson(json);

        /// <summary>
        /// 序列化为Json
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>Json</returns>
        public string ExportToJson(bool format) =>
            JsonConvert.SerializeObject(this, format ? Formatting.Indented : Formatting.None);


        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
        public static Chart LoadFromJson(string json) => JsonConvert.DeserializeObject<Chart>(json) ??
                                                         throw new InvalidOperationException(
                                                             "Failed to deserialize Chart from JSON.");

        /// <summary>
        /// 异步序列化为Json
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>json</returns>
        public Task<string> ExportToJsonAsync(bool format)
            => Task.Run(() => ExportToJson(format));


        /// <summary>
        /// 异步从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        public static Task<Chart> LoadFromJsonAsync(string json)
            => Task.Run(() => LoadFromJson(json));


        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 675f;
            public const float MinX = -675f;
            public const float MaxY = 450f;
            public const float MinY = -450f;
        }

        /// <summary>
        /// BPM列表
        /// </summary>
        [JsonProperty("BPMList")] public Bpm[] BpmList =
        {
            new Bpm()
        };

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("META")] public Meta Meta = new Meta();

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")] public JudgeLine[] JudgeLineList = { };

        /// <summary>
        /// 制谱时长（秒）
        /// </summary>
        [JsonProperty("chartTime")] public double ChartTime = 0d;

        /// <summary>
        /// 判定线组
        /// </summary>
        [JsonProperty("judgeLineGroup")] public string[] JudgeLineGroup = { "Default" };

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
}