using System;
using System.Text;

namespace KaedePhi.Core.PhiEdit
{
    public class Note
    {
        public NoteType Type { get; set; } = NoteType.Tap;
        public float StartBeat { get; set; }
        public float EndBeat { get; set; } // 仅 Hold 使用
        public bool Above { get; set; } = true;
        public float PositionX { get; set; }
        public float WidthRatio { get; set; } = 1.0f;
        public bool IsFake { get; set; }
        public float SpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int)"/>
        /// </summary>
        public override string ToString()
            => $"Note(Type: {Type}, StartBeat: {StartBeat}, EndBeat: {EndBeat}, Above: {Above}, PositionX: {PositionX}, WidthRatio: {WidthRatio}, IsFake: {IsFake}, SpeedMultiplier: {SpeedMultiplier})";


        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        /// <exception cref="ArgumentException">存储的数值有误</exception>
        public string ToString(int judgeLineIndex)
        {
            const int fakeNote = 1;
            const int realNote = 0;
            const int aboveNote = 1;
            const int belowNote = 2;
            var stringBuilder = new StringBuilder();
            if (Type != NoteType.Hold)
            {
                if (Math.Abs(StartBeat - EndBeat) > 0.0001f) // 两者不相等？这不是Hold吧，throw
                    throw new ArgumentException("非Hold音符的开始拍与结束拍应相等");


                var aboveNumber = Above ? aboveNote : belowNote; // 上方为1，下方为2
                var isFakeNumber = IsFake ? fakeNote : realNote; // 假音符为1，真音符为0
                stringBuilder.AppendLine(
                    $"n{(int)Type} {judgeLineIndex} {StartBeat} {PositionX} {aboveNumber} {isFakeNumber}");
            }
            else
            {
                var aboveNumber = Above ? aboveNote : belowNote; // 上方为1，下方为2
                var isFakeNumber = IsFake ? fakeNote : realNote; // 假音符为1，真音符为0
                stringBuilder.AppendLine(
                    $"n{(int)Type} {judgeLineIndex} {StartBeat} {EndBeat} {PositionX} {aboveNumber} {isFakeNumber}");
            }

            stringBuilder.AppendLine($"# {SpeedMultiplier}");
            stringBuilder.AppendLine($"& {WidthRatio}");

            return stringBuilder.ToString().Trim();
        }

        public Note Clone()
        {
            return new Note
            {
                Type = Type,
                StartBeat = StartBeat,
                EndBeat = EndBeat,
                Above = Above,
                PositionX = PositionX,
                WidthRatio = WidthRatio,
                IsFake = IsFake,
                SpeedMultiplier = SpeedMultiplier
            };
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