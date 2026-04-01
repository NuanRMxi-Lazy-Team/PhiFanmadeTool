using System.Collections.Generic;
using System.Linq;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 2048f;
            public const float MinX = 0f;
            public const float MaxY = 1400f;
            public const float MinY = 0f;
            public const bool ClockwiseRotation = false;
        }

        /// <summary>
        /// 判定线列表
        /// </summary>
        public List<JudgeLine> JudgeLineList { get; set; } = new List<JudgeLine>();

        /// <summary>
        /// BPM列表
        /// </summary>
        public List<BpmItem> BpmList { get; set; } = new List<BpmItem>();

        public Chart Clone()
        {
            var clonedChart = new Chart
            {
                Offset = Offset,
                BpmList = BpmList.Select(b => b.Clone()).ToList(),
                JudgeLineList = JudgeLineList.Select(jl => jl.Clone()).ToList()
            };
            return clonedChart;
        }
    }
}