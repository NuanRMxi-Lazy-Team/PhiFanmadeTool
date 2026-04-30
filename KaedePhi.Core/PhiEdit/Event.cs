using System;

namespace KaedePhi.Core.PhiEdit
{
    public class Event
    {
        public float StartBeat { get; set; }
        public float EndBeat { get; set; }
        public Easing EasingType { get; set; } = new(1);
        public float EndValue { get; set; }

        /// <summary>
        /// 获取某个拍在这个事件中的值
        /// </summary>
        /// <param name="beat">指定拍</param>
        /// <param name="startValue">开始数值</param>
        /// <returns>当前数值</returns>
        public float GetValueAtBeat(float beat, float startValue)
        {
            //获得这个拍在这个事件的时间轴上的位置
            float t = (beat - StartBeat) / (EndBeat - StartBeat);
            return EasingType.Interpolate(startValue, EndValue, t);
        }

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int, string)"/>
        /// </summary>
        public override string ToString()
            => $"Event(StartBeat={StartBeat}, EndBeat={EndBeat}, EasingType={EasingType}, EndValue={EndValue})";


        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <param name="head">格式头</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex, string head)
        {
            if (head is "cm" or "cp")
                throw new ArgumentException("请使用 MoveEvent 或 MoveFrame 的 ToString 方法，这不是一个 MoveEvent 或 MoveFrame");
            return head != "cf"
                ? $"{head} {judgeLineIndex} {StartBeat} {EndBeat} {EndValue} {(int)EasingType}"
                : $"{head} {judgeLineIndex} {StartBeat} {EndBeat} {EndValue}";
        }

        public Event Clone()
        {
            return new Event
            {
                StartBeat = StartBeat,
                EndBeat = EndBeat,
                EasingType = EasingType,
                EndValue = EndValue
            };
        }
    }
}