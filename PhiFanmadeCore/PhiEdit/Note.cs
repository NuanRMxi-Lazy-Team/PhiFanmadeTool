using System;
using System.Text;

namespace PhiFanmade.Core.PhiEdit
{
    public class Note
    {
        public NoteType Type = NoteType.Tap;
        public float StartBeat = 0f;
        public float EndBeat = 0f; // 仅 Hold 使用
        public bool Above = true;
        public float PositionX = 0.0f;
        public float WidthRatio = 1.0f;
        public bool IsFake = false;
        public float SpeedMultiplier = 1.0f;

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
        /// <exception cref="ArgumentException">存储的数值有误</exception>
        public string ToString(int judgeLineIndex)
        {
            var stringBuilder = new StringBuilder();
            if (Type != NoteType.Hold)
            {
                if (Math.Abs(StartBeat - EndBeat) > 0.0001f) // 两者不相等？这不是Hold吧，throw
                    throw new ArgumentException("非Hold音符的开始拍与结束拍应相等");
                

                var aboveNumber = Above ? 1 : 2; // 上方为1，下方为2
                var isFakeNumber = IsFake ? 1 : 0; // 假音符为1，真音符为0
                stringBuilder.AppendLine(
                    $"n{(int)Type} {judgeLineIndex} {StartBeat} {PositionX} {aboveNumber} {isFakeNumber}");
                stringBuilder.AppendLine($"# {WidthRatio}");
                stringBuilder.AppendLine($"& {SpeedMultiplier}");
            }
            else
            {
                var aboveNumber = Above ? 1 : 2; // 上方为1，下方为2
                var isFakeNumber = IsFake ? 1 : 0; // 假音符为1，真音符为0
                stringBuilder.AppendLine(
                    $"n{(int)Type} {judgeLineIndex} {StartBeat} {EndBeat} {PositionX} {aboveNumber} {isFakeNumber}");
                stringBuilder.AppendLine($"# {WidthRatio}");
                stringBuilder.AppendLine($"& {SpeedMultiplier}");
            }

            return stringBuilder.ToString().Trim();
        }
    }

    public enum NoteType
    {
        Tap = 1,
        Hold = 2,
        Flick = 3,
        Drag = 4
    }
}