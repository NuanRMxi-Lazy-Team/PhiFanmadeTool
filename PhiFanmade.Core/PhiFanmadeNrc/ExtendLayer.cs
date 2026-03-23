using System.Collections.Generic;

// STJ 特性使用完全限定名

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public class ExtendLayer
    {
        /// <summary>
        /// 判定线纹理颜色事件列表，颜色格式为RGB字节数组，使用顶点颜色乘法
        /// </summary>
        public List<Event<byte[]>> ColorEvents;

        /// <summary>
        /// 判定线纹理宽度缩放事件列表
        /// </summary>
        public List<Event<float>> ScaleXEvents;

        /// <summary>
        /// 判定线纹理高度缩放事件列表
        /// </summary>
        public List<Event<float>> ScaleYEvents;

        /// <summary>
        /// 判定线文字纹理事件列表
        /// </summary>
        public List<Event<string>> TextEvents;

        /// <summary>
        /// 画笔事件列表，值为画笔大小
        /// </summary>
        public List<Event<float>> PaintEvents;

        /// <summary>
        /// 判定线动图播放进度事件列表，值为动图帧进度（0~1之间）
        /// </summary>
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
            if (ColorEvents is { Count: 0 })
                ColorEvents = null;
            if (ScaleXEvents is { Count: 0 })
                ScaleXEvents = null;
            if (ScaleYEvents is { Count: 0 })
                ScaleYEvents = null;
            if (TextEvents is { Count: 0 })
                TextEvents = null;
            if (PaintEvents is { Count: 0 })
                PaintEvents = null;
            if (GifEvents is { Count: 0 })
                GifEvents = null;
        }
    }
}