using System;

namespace PhiFanmade.Core.PhiEdit
{
    public static partial class PhiEdit
    {
        public class MoveFrame
        {
            public float Beat = 0f;
            public float XValue = 0f;
            public float YValue = 0f;

            /// <summary>
            /// DO NOT USE THIS METHOD
            /// </summary>
            /// <returns>DO NOT USE THIS METHOD</returns>
            /// <exception cref="ArgumentException">BECAUSE YOU USED A REMOVED METHOD</exception>
            [Obsolete("请使用 ToString(int judgeLineIndex) 方法", true)]
            public override string ToString()
                => throw new ArgumentException("请使用 ToString(int judgeLineIndex) 方法");


            /// <summary>
            /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
            /// </summary>
            /// <param name="judgeLineIndex">判定线索引</param>
            /// <returns>PhiEditor Chart格式字符串</returns>
            public string ToString(int judgeLineIndex)
                => $"cp {judgeLineIndex} {Beat} {XValue} {YValue}";
        }
    }
}