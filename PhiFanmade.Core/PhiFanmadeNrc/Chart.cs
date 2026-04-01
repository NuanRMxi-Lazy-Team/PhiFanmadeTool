using System.Collections.Generic;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public partial class Chart
    {
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 1f;
            public const float MinX = -1f;
            public const float MaxY = 1f;
            public const float MinY = -1f;
            public const bool ClockwiseRotation = false;
        }

        /// <summary>
        /// BPM列表
        /// </summary>
        public List<BpmItem> BpmList { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public Meta Meta { get; set; } = new Meta();

        /// <summary>
        /// 判定线列表
        /// </summary>
        public List<JudgeLine> JudgeLineList { get; set; }
    }
}