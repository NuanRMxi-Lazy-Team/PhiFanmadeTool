using System;
namespace PhiFanmade.Core.PhiEdit
{
    public class Event
    {
        public float StartBeat = 0f;
        public float EndBeat = 0f;
        public Easing EasingType = new Easing(1);
        public float EndValue = 0f;
        
        /// <summary>
        /// 获取某个拍在这个事件中的值
        /// </summary>
        /// <param name="beat">指定拍</param>
        /// <param name="startValue">开始数值</param>
        /// <returns>当前数值</returns>
        public float GetValueAtBeat(float beat,float startValue)
        {
            //获得这个拍在这个事件的时间轴上的位置
            float t = (beat - StartBeat) / (EndBeat - StartBeat);
            return EasingType.Do(0, 1, startValue, EndValue, t);
        }
        
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
            if (head == "cm" || head == "cp")
                throw new ArgumentException("请使用 MoveEvent 或 MoveFrame 的 ToString 方法，这不是一个 MoveEvent 或 MoveFrame");
            if (head != "cf")
                return $"{head} {judgeLineIndex} {StartBeat} {EndBeat} {EndValue} {(int)EasingType}";
            else
                return $"{head} {judgeLineIndex} {StartBeat} {EndBeat} {EndValue}";
        }
    }
}