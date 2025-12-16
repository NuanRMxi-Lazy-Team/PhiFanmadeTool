using System;
using System.Collections.Generic;
using Newtonsoft.Json;
// STJ 特性使用完全限定名

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
            [Newtonsoft.Json.JsonConverter(typeof(ColorEventsConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("colorEvents"), System.Text.Json.Serialization.JsonConverter(typeof(StjColorEventsConverter))]
#endif
            public List<Event<byte[]>> ColorEvents;
            
            /// <summary>
            /// 判定线纹理宽度缩放事件列表
            /// </summary>
            [JsonProperty("scaleXEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("scaleXEvents")]
#endif
            public List<Event<float>> ScaleXEvents;
            
            /// <summary>
            /// 判定线纹理高度缩放事件列表
            /// </summary>
            [JsonProperty("scaleYEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("scaleYEvents")]
#endif
            public List<Event<float>> ScaleYEvents;
            
            /// <summary>
            /// 判定线文字纹理事件列表
            /// </summary>
            [JsonProperty("textEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("textEvents")]
#endif
            public List<Event<string>> TextEvents;
            
            /// <summary>
            /// 画笔事件列表，值为画笔大小
            /// </summary>
            [JsonProperty("paintEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("paintEvents")]
#endif
            public List<Event<float>> PaintEvents;
            
            /// <summary>
            /// 判定线动图播放进度事件列表，值为动图帧进度（0~1之间）
            /// </summary>
            [JsonProperty("gifEvents", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("gifEvents")]
#endif
            public List<Event<float>> GifEvents;

            public ExtendLayer Clone()
            {
                // 深拷贝，包括Event列表
                var clone = new ExtendLayer();
                // 保证列表中的元素也被深拷贝（通过LINQ调用Event的Clone方法）
                if (ColorEvents != null)
                    clone.ColorEvents = ColorEvents.ConvertAll(e => e.Clone());
                if (ScaleXEvents != null)
                    clone.ScaleXEvents = ScaleXEvents.ConvertAll(e => e.Clone());
                if (ScaleYEvents != null)
                    clone.ScaleYEvents = ScaleYEvents.ConvertAll(e => e.Clone());
                if (TextEvents != null)
                    clone.TextEvents = TextEvents.ConvertAll(e => e.Clone());
                if (PaintEvents != null)
                    clone.PaintEvents = PaintEvents.ConvertAll(e => e.Clone());
                if (GifEvents != null)
                    clone.GifEvents = GifEvents.ConvertAll(e => e.Clone());
                return clone;
            }
            
            /// <summary>
            /// 强行预期化，将空列表设置为null，保证Json序列化时不包含空列表
            /// </summary>
            public void Anticipation()
            {
                if (ColorEvents != null && ColorEvents.Count == 0)
                    ColorEvents = null;
                if (ScaleXEvents != null && ScaleXEvents.Count == 0)
                    ScaleXEvents = null;
                if (ScaleYEvents != null && ScaleYEvents.Count == 0)
                    ScaleYEvents = null;
                if (TextEvents != null && TextEvents.Count == 0)
                    TextEvents = null;
                if (PaintEvents != null && PaintEvents.Count == 0)
                    PaintEvents = null;
                if (GifEvents != null && GifEvents.Count == 0)
                    GifEvents = null;
            }
        }
    }
}