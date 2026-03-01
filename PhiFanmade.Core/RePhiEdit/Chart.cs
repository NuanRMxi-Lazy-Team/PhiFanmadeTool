using System.Collections.Generic;
using Newtonsoft.Json;
#if !NETSTANDARD2_1
using System.Text.Json.Serialization;
#endif

namespace PhiFanmade.Core.RePhiEdit
{
    public partial class Chart
    {


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
        [JsonProperty("BPMList")]
#if !NETSTANDARD2_1
        [JsonPropertyName("BPMList")]
#endif
        public List<Bpm> BpmList = new List<Bpm>
        {
            new Bpm()
        };

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("META")]
#if !NETSTANDARD2_1
        [JsonPropertyName("META")]
#endif
        public Meta Meta = new Meta();

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")]
#if !NETSTANDARD2_1
        [JsonPropertyName("judgeLineList")]
#endif
        public List<JudgeLine> JudgeLineList = new List<JudgeLine>();

        /// <summary>
        /// 制谱时长（秒）
        /// </summary>
        [JsonProperty("chartTime")]
#if !NETSTANDARD2_1
        [JsonPropertyName("chartTime")]
#endif
        public double ChartTime = 0d;

        /// <summary>
        /// 判定线组
        /// </summary>
        [JsonProperty("judgeLineGroup")]
#if !NETSTANDARD2_1
        [JsonPropertyName("judgeLineGroup")]
#endif
        public string[] JudgeLineGroup = { "Default" };

        /// <summary>
        /// 多线编辑判定线列表（以空格为分割，或使用x:y选中x~y所有判定线）
        /// </summary>
        [JsonProperty("multiLineString")]
#if !NETSTANDARD2_1
        [JsonPropertyName("multiLineString")]
#endif
        public string MultiLineString = "1";

        /// <summary>
        /// 多线编辑页面缩放比例
        /// </summary>
        [JsonProperty("multiScale")]
#if !NETSTANDARD2_1
        [JsonPropertyName("multiScale")]
#endif
        public float MultiScale = 1.0f;

        /// <summary>
        /// XY事件是否一一对应
        /// </summary>
        [JsonProperty("xybind")]
#if !NETSTANDARD2_1
        [JsonPropertyName("xybind")]
#endif
        public bool XyBind = true;
    }
}