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

                if (string.IsNullOrWhiteSpace(line)) continue;

                var part = line.Split(' ');
                var judgeLineIndex = part.Length > 1 ? int.Parse(part[1]) : -1;

                // 本地函数：确保判定线存在
                void EnsureJudgeLineExists()
                {
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                }

                switch (part.First())
                {
                    case "bp":
                        chart.BpmList.Add(new Bpm
                        {
                            StartBeat = float.Parse(part[1]),
                            BeatPerMinute = float.Parse(part[2])
                        });
                        break;

                    case "cv":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].SpeedFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "cp":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].MoveFrames.Add(new MoveFrame
                        {
                            Beat = float.Parse(part[2]),
                            XValue = float.Parse(part[3]),
                            YValue = float.Parse(part[4])
                        });
                        break;

                    case "cd":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].RotateFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "ca":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].AlphaFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "cm":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].MoveEvents.Add(new MoveEvent
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndXValue = float.Parse(part[4]),
                            EndYValue = float.Parse(part[5]),
                            EasingType = new Easing(int.Parse(part[6]))
                        });
                        break;

                    case "cr":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].RotateEvents.Add(new Event
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndValue = float.Parse(part[4]),
                            EasingType = new Easing(int.Parse(part[5]))
                        });
                        break;

                    case "cf":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].AlphaEvents.Add(new Event
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndValue = float.Parse(part[4]),
                            EasingType = new Easing(1)
                        });
                        break;

                    default:
                        if (line.StartsWith("n"))
                        {
                            var noteType = (NoteType)int.Parse(part[0].Substring(1, 1));
                            var noteSpeedMultiplierPart = lines[i + 1].Split(' ');
                            var noteWidthRatioPart = lines[i + 2].Split(' ');

                            var note = new Note
                            {
                                StartBeat = float.Parse(part[2]),
                                EndBeat = noteType == NoteType.Hold ? float.Parse(part[3]) : float.Parse(part[2]),
                                PositionX = float.Parse(part[noteType == NoteType.Hold ? 4 : 3]),
                                Above = part[noteType == NoteType.Hold ? 5 : 4] == "1",
                                IsFake = part[noteType == NoteType.Hold ? 6 : 5] == "1",
                                SpeedMultiplier = float.Parse(noteSpeedMultiplierPart[1]),
                                WidthRatio = float.Parse(noteWidthRatioPart[1]),
                                Type = noteType
                            };

                            EnsureJudgeLineExists();
                            judgeDict[judgeLineIndex].NoteList.Add(note);
                            i += 2;
                        }

                        break;
                }
            }

            foreach (var judgeLine in judgeDict.Values)
            {
                // 排序
                // Frame
                judgeLine.SpeedFrames = judgeLine.SpeedFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.MoveFrames = judgeLine.MoveFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.RotateFrames = judgeLine.RotateFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.AlphaFrames = judgeLine.AlphaFrames.OrderBy(f => f.Beat).ToList();
                // Event
                judgeLine.MoveEvents = judgeLine.MoveEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.RotateEvents = judgeLine.RotateEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.AlphaEvents = judgeLine.AlphaEvents.OrderBy(e => e.StartBeat).ToList();
                // Note
                judgeLine.NoteList = judgeLine.NoteList.OrderBy(n => n.StartBeat).ToList();
                // BPM
                chart.BpmList = chart.BpmList.OrderBy(b => b.StartBeat).ToList();
            }

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