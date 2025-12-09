using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhiFanmade.Core.PhiEdit
{
    public class Chart
    {
        public static Chart Load(string pec)
        {
            var chart = new Chart();
            var judgeDict = new Dictionary<int, JudgeLine>();
            // 逐行解析
            var lines = pec.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i == 0)
                {
                    if (!int.TryParse(line, out chart.Offset))
                        throw new FormatException("Malformed chart file: first line is not a valid integer offset.");
                    continue;

                }
                // 跳过空行或只包含空白字符的行
                if (string.IsNullOrWhiteSpace(line))
                    continue; 

                // 以空格分割line
                var part = line.Split(' ');
                if (part.First() == "bp")
                {
                    chart.BpmList.Add(new Bpm
                    {
                        StartBeat = float.Parse(part[1]),
                        BeatPerMinute = float.Parse(part[2])
                    });
                    continue;
                }

                if (part.First() == "cv")
                {
                    var speedFrame = new Frame
                    {
                        Beat = float.Parse(part[2]),
                        Value = float.Parse(part[3])
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].SpeedFrames.Add(speedFrame);
                    continue;
                }

                if (part.First() == "cp")
                {
                    var moveFrame = new MoveFrame
                    {
                        Beat = float.Parse(part[2]),
                        XValue = float.Parse(part[3]),
                        YValue = float.Parse(part[4])
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].MoveFrames.Add(moveFrame);
                    continue;
                }

                if (part.First() == "cd")
                {
                    var rotateFrame = new Frame
                    {
                        Beat = float.Parse(part[2]),
                        Value = float.Parse(part[3])
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].RotateFrames.Add(rotateFrame);
                    continue;
                }

                if (part.First() == "ca")
                {
                    var alphaFrame = new Frame()
                    {
                        Beat = float.Parse(part[2]),
                        Value = float.Parse(part[3])
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].AlphaFrames.Add(alphaFrame);
                    continue;
                }

                if (part.First() == "cm")
                {
                    var moveEvent = new MoveEvent
                    {
                        StartBeat = float.Parse(part[2]),
                        EndBeat = float.Parse(part[3]),
                        EndXValue = float.Parse(part[4]),
                        EndYValue = float.Parse(part[5]),
                        EasingType = new Easing(int.Parse(part[6]))
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].MoveEvents.Add(moveEvent);
                    continue;
                }

                if (part.First() == "cr")
                {
                    var rotateEvent = new Event
                    {
                        StartBeat = float.Parse(part[2]),
                        EndBeat = float.Parse(part[3]),
                        EndValue = float.Parse(part[4]),
                        EasingType = new Easing(int.Parse(part[5]))
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].RotateEvents.Add(rotateEvent);
                    continue;
                }

                if (part.First() == "cf")
                {
                    var alphaEvent = new Event
                    {
                        StartBeat = float.Parse(part[2]),
                        EndBeat = float.Parse(part[3]),
                        EndValue = float.Parse(part[4]),
                        EasingType = new Easing(1)
                    };
                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].AlphaEvents.Add(alphaEvent);
                    continue;
                }

                if (line.StartsWith("n"))
                {
                    // 读取第二个字符为音符类型
                    var noteType = (NoteType)int.Parse(part[0].Substring(1, 1));
                    // 读取当前行后面两行，应该以“#”与“&”开头的两行，不是同一行
                    var noteSpeedMultiplierLine = lines[i + 1];
                    var noteSpeedMultiplierPart = noteSpeedMultiplierLine.Split(' ');
                    var noteWidthRatioLine = lines[i + 2];
                    var noteWidthRatioPart = noteWidthRatioLine.Split(' ');
                    Note note;
                    if (noteType != NoteType.Hold)
                    {
                        note = new Note
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[2]),
                            PositionX = float.Parse(part[3]),
                            Above = part[4] == "1",
                            IsFake = part[5] == "1",
                            SpeedMultiplier = float.Parse(noteSpeedMultiplierPart[1]),
                            WidthRatio = float.Parse(noteWidthRatioPart[1]),
                            Type = noteType
                        };
                    }
                    else
                    {
                        note = new Note
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            PositionX = float.Parse(part[4]),
                            Above = part[5] == "1",
                            IsFake = part[6] == "1",
                            SpeedMultiplier = float.Parse(noteSpeedMultiplierPart[1]),
                            WidthRatio = float.Parse(noteWidthRatioPart[1]),
                            Type = noteType
                        };
                    }

                    var judgeLineIndex = int.Parse(part[1]);
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                    judgeDict[judgeLineIndex].NoteList.Add(note);
                    // 跳过后面两行
                    i += 2;
                }
            }

            // 把什么Frame啊Event啊的东西，按StartBeat或Beat排序
            foreach (var judgeLine in judgeDict.Values)
            {
                judgeLine.SpeedFrames = judgeLine.SpeedFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.MoveFrames = judgeLine.MoveFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.RotateFrames = judgeLine.RotateFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.AlphaFrames = judgeLine.AlphaFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.MoveEvents = judgeLine.MoveEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.RotateEvents = judgeLine.RotateEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.AlphaEvents = judgeLine.AlphaEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.NoteList = judgeLine.NoteList.OrderBy(n => n.StartBeat).ToList();
            }

            // 将字典转换为列表，并按键值排序
            chart.JudgeLineList = judgeDict.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
            return chart;
        }

        public static async Task<Chart> LoadAsync(string pec)
            => await Task.Run(() => Load(pec));


        public string Export()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Offset.ToString());
            foreach (var bpm in BpmList)
                stringBuilder.AppendLine(bpm.ToString());
            for (int i = 0; i < JudgeLineList.Count; i++)
            {
                var judgeLine = JudgeLineList[i];
                // Frame
                foreach (var moveFrame in judgeLine.MoveFrames)
                    stringBuilder.AppendLine(moveFrame.ToString(i));
                foreach (var speedFrame in judgeLine.SpeedFrames)
                    stringBuilder.AppendLine(speedFrame.ToString(i, "cv"));
                foreach (var rotateFrame in judgeLine.RotateFrames)
                    stringBuilder.AppendLine(rotateFrame.ToString(i, "cd"));
                foreach (var alphaFrame in judgeLine.AlphaFrames)
                    stringBuilder.AppendLine(alphaFrame.ToString(i, "ca"));
                // Event
                foreach (var moveEvent in judgeLine.MoveEvents)
                    stringBuilder.AppendLine(moveEvent.ToString(i));
                foreach (var rotateEvent in judgeLine.RotateEvents)
                    stringBuilder.AppendLine(rotateEvent.ToString(i, "cr"));
                foreach (var alphaEvent in judgeLine.AlphaEvents)
                    stringBuilder.AppendLine(alphaEvent.ToString(i, "cf"));
                // Note
                foreach (var note in judgeLine.NoteList)
                    stringBuilder.AppendLine(note.ToString(i));
            }

            return stringBuilder.ToString().Trim();
        }

        public async Task<string> ExportAsync()
            => await Task.Run(() => Export());


        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        public int Offset = 0;

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 1024f;
            public const float MinX = -1024f;
            public const float MaxY = 700f;
            public const float MinY = -700f;
        }

        public List<JudgeLine> JudgeLineList = new List<JudgeLine>();
        public List<Bpm> BpmList = new List<Bpm>();
    }
}