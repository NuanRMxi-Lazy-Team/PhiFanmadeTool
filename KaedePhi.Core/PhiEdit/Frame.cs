using System;

namespace KaedePhi.Core.PhiEdit
{
    public class Frame
    {
        public float Beat { get; set; }
        public float Value { get; set; }

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int, string)"/>
        /// </summary>
        public override string ToString()
            => $"Frame(Beat={Beat}, Value={Value})";
        
        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <param name="head">格式头</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex, string head)
        {
            return head is "cp" or "cm"
                ? throw new ArgumentException("请使用 MoveFrame 或 MoveEvent 的 ToString 方法，这不是一个 MoveFrame 或 MoveEvent")
                : $"{head} {judgeLineIndex} {Beat} {Value}";
        }

        public Frame Clone()
        {
            return new Frame
            {
                Beat = Beat,
                Value = Value
            };
        }
    }
}