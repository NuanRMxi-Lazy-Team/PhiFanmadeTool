using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class ExtendLayer
        {
            /// <summary>
            /// 判定线纹理颜色事件列表，颜色格式为RGB字节数组，使用顶点颜色乘法
            /// </summary>
            [JsonProperty("colorEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<byte[]>> ColorEvents = new List<Event<byte[]>>();

            /// <summary>
            /// 判定线纹理宽度缩放事件列表
            /// </summary>
            [JsonProperty("scaleXEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> ScaleXEvents = new List<Event<float>>();

            /// <summary>
            /// 判定线纹理高度缩放事件列表
            /// </summary>
            [JsonProperty("scaleYEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> ScaleYEvents = new List<Event<float>>();

            /// <summary>
            /// 判定线文字纹理事件列表
            /// </summary>
            [JsonProperty("textEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<string>> TextEvents = new List<Event<string>>();

            /// <summary>
            /// 画笔事件列表，值为画笔大小
            /// </summary>
            [JsonProperty("paintEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> PaintEvents = new List<Event<float>>();

            /// <summary>
            /// 判定线动图播放进度事件列表，值为动图帧进度（0~1之间）
            /// </summary>
            [JsonProperty("gifEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<Event<float>> GifEvents = new List<Event<float>>();
        }
    }
}