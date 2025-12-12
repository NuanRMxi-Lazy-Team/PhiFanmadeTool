using System;

namespace PhiFanmade.Core.PhiEdit
{
    public static partial class PhiEdit
    {
        public class MoveEvent
        {
            public float StartBeat = 0f;
            public float EndBeat = 0f;
            public Easing EasingType = new Easing(1);
            public float EndXValue = 0f;
            public float EndYValue = 0f;

            /// <summary>
            /// 获取某个拍在这个事件中的值
            /// </summary>
            /// <param name="beat">指定拍</param>
            /// <param name="startXValue">开始X坐标</param>
            /// <param name="startYValue">开始Y坐标</param>
            /// <returns>当前坐标（x,y）</returns>
            public (float, float) GetValueAtBeat(float beat, float startXValue, float startYValue)
            {
                //获得这个拍在这个事件的时间轴上的位置
                float t = (beat - StartBeat) / (EndBeat - StartBeat);
                var xValue = EasingType.Do(0, 1, startXValue, EndXValue, t);
                var yValue = EasingType.Do(0, 1, startYValue, EndYValue, t);
                return (xValue, yValue);
            }

            /// <summary>
            /// DO NOT USE THIS METHOD
            /// </summary>
            /// <returns>DO NOT USE THIS METHOD</returns>
            /// <exception cref="ArgumentException">BECAUSE YOU USED A REMOVED METHOD</exception>
            public override string ToString()
                => throw new ArgumentException("请使用 ToString(int judgeLineIndex) 方法");


            /// <summary>
            /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
            /// </summary>
            /// <param name="judgeLineIndex">判定线索引</param>
            /// <returns>PhiEditor Chart格式字符串</returns>
            public string ToString(int judgeLineIndex)
                => $"cm {judgeLineIndex} {StartBeat} {EndBeat} {EndXValue} {EndYValue} {(int)EasingType}";
        }
    }
}