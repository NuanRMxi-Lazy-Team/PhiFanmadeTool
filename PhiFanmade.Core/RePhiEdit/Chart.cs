using System.Collections.Generic;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;

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
            public const bool ClockwiseRotation = true;
        }

        /// <summary>
        /// BPM列表
        /// </summary>
        [JsonProperty("BPMList")] public List<BpmItem> BpmList { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("META")] public Meta Meta = new Meta();

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")] public List<JudgeLine> JudgeLineList { get; set; }

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