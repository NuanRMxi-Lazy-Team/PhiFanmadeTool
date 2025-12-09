using System;

namespace PhiFanmade.Core.PhiEdit
{
    public class Frame
    {
        public float Beat = 0f;
        public float Value = 0f;

        /// <summary>
        /// DO NOT USE THIS METHOD
        /// </summary>
        /// <returns>DO NOT USE THIS METHOD</returns>
        /// <exception cref="ArgumentException">BECAUSE YOU USED A REMOVED METHOD</exception>
        [Obsolete("请使用 ToString(int judgeLineIndex, string head) 方法", true)]
        public override string ToString()
            => throw new ArgumentException("请使用 ToString(int judgeLineIndex, string head) 方法");


        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <param name="head">格式头</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex, string head)
        {
            if (head == "cp" || head == "cm")
                throw new ArgumentException("请使用 MoveFrame 或 MoveEvent 的 ToString 方法，这不是一个 MoveFrame 或 MoveEvent");
            return $"{head} {judgeLineIndex} {Beat} {Value}";
        }
    }
}