using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PhiFanmade.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 从PhiEditChart格式的字符串加载谱面
        /// </summary>
        /// <param name="pec">PhiEditChart字符串</param>
        /// <returns>已反序列化谱面</returns>
        /// <exception cref="FormatException">格式不正确</exception>
        [PublicAPI]
        public static Chart Load(string pec)
        {
            var chart = new Chart();
            var judgeDict = new Dictionary<int, JudgeLine>();
            var lines = pec.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (!int.TryParse(lines[0], out var offset))
                throw new FormatException("Malformed chart file: first line is not a valid integer offset.");
            chart.Offset = offset;

            var i = 1;
            while (i < lines.Length)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                var part = line.Split(' ');
                int judgeLineIndex = -1;
                if (part[0] != "bp")
                    judgeLineIndex = part.Length > 1 ? int.Parse(part[1]) : -1;

                if (part[0] == "bp")
                {
                    chart.BpmList.Add(new BpmItem
                    {
                        StartBeat = float.Parse(part[1]),
                        Bpm = float.Parse(part[2])
                    });
                    i++;
                }
                else if (line.StartsWith('n'))
                {
                    i = ParseNote(lines, i, part, judgeLineIndex, judgeDict);
                    i++;
                }
                else
                {
                    ParseLineCommand(part, judgeLineIndex, judgeDict);
                    i++;
                }
            }

            SortAndBuild(chart, judgeDict);
            return chart;
        }

        private static void ParseLineCommand(string[] part, int judgeLineIndex, Dictionary<int, JudgeLine> judgeDict)
        {
            switch (part[0])
            {
                case "cv":
                    Ensure();
                    judgeDict[judgeLineIndex].SpeedFrames.Add(new Frame
                        { Beat = float.Parse(part[2]), Value = float.Parse(part[3]) });
                    break;
                case "cp":
                    Ensure();
                    judgeDict[judgeLineIndex].MoveFrames.Add(new MoveFrame
                        { Beat = float.Parse(part[2]), XValue = float.Parse(part[3]), YValue = float.Parse(part[4]) });
                    break;
                case "cd":
                    Ensure();
                    judgeDict[judgeLineIndex].RotateFrames.Add(new Frame
                        { Beat = float.Parse(part[2]), Value = float.Parse(part[3]) });
                    break;
                case "ca":
                    Ensure();
                    judgeDict[judgeLineIndex].AlphaFrames.Add(new Frame
                        { Beat = float.Parse(part[2]), Value = float.Parse(part[3]) });
                    break;
                case "cm":
                    Ensure();
                    judgeDict[judgeLineIndex].MoveEvents.Add(new MoveEvent
                    {
                        StartBeat = float.Parse(part[2]), EndBeat = float.Parse(part[3]),
                        EndXValue = float.Parse(part[4]), EndYValue = float.Parse(part[5]),
                        EasingType = new Easing(int.Parse(part[6]))
                    });
                    break;
                case "cr":
                    Ensure();
                    judgeDict[judgeLineIndex].RotateEvents.Add(new Event
                    {
                        StartBeat = float.Parse(part[2]), EndBeat = float.Parse(part[3]),
                        EndValue = float.Parse(part[4]), EasingType = new Easing(int.Parse(part[5]))
                    });
                    break;
                case "cf":
                    Ensure();
                    judgeDict[judgeLineIndex].AlphaEvents.Add(new Event
                    {
                        StartBeat = float.Parse(part[2]), EndBeat = float.Parse(part[3]),
                        EndValue = float.Parse(part[4]), EasingType = new Easing(1)
                    });
                    break;
            }

            return;

            void Ensure()
            {
                if (!judgeDict.ContainsKey(judgeLineIndex)) judgeDict[judgeLineIndex] = new JudgeLine();
            }
        }

        private static int ParseNote(string[] lines, int i, string[] part, int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict)
        {
            var noteType = (NoteType)int.Parse(part[0].Substring(1, 1));
            var hashIndex = Array.IndexOf(part, "#");
            var ampIndex = Array.IndexOf(part, "&");

            string[] noteSpeedMultiplierPart;
            string[] noteWidthRatioPart;
            if (hashIndex != -1 && ampIndex != -1)
            {
                noteSpeedMultiplierPart = new[] { "#", part[hashIndex + 1] };
                noteWidthRatioPart = new[] { "&", part[ampIndex + 1] };
            }
            else
            {
                noteSpeedMultiplierPart = lines[i + 1].Split(' ');
                noteWidthRatioPart = lines[i + 2].Split(' ');
                i += 2;
            }

            var isHold = noteType == NoteType.Hold;
            var note = new Note
            {
                StartBeat = float.Parse(part[2]),
                EndBeat = isHold ? float.Parse(part[3]) : float.Parse(part[2]),
                PositionX = float.Parse(part[isHold ? 4 : 3]),
                Above = part[isHold ? 5 : 4] == "1",
                IsFake = part[isHold ? 6 : 5] == "1",
                SpeedMultiplier = float.Parse(noteSpeedMultiplierPart[1]),
                WidthRatio = float.Parse(noteWidthRatioPart[1]),
                Type = noteType
            };

            if (!judgeDict.ContainsKey(judgeLineIndex)) judgeDict[judgeLineIndex] = new JudgeLine();
            judgeDict[judgeLineIndex].NoteList.Add(note);
            return i;
        }

        private static void SortAndBuild(Chart chart, Dictionary<int, JudgeLine> judgeDict)
        {
            chart.BpmList = chart.BpmList.OrderBy(b => b.StartBeat).ToList();
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
        }

        /// <summary>
        /// 异步从PhiEditChart格式的字符串加载谱面
        /// </summary>
        /// <param name="pec">PhiEditChart字符串</param>
        /// <returns>已反序列化谱面</returns>
        public static async Task<Chart> LoadAsync(string pec)
            => await Task.Run(() => Load(pec));


        /// <summary>
        /// 枚举单条判定线的所有导出行
        /// </summary>
        private static IEnumerable<string> GetJudgeLineLines(JudgeLine judgeLine, int index)
        {
            // Frame
            foreach (var frame in judgeLine.MoveFrames) yield return frame.ToString(index);
            foreach (var frame in judgeLine.SpeedFrames) yield return frame.ToString(index, "cv");
            foreach (var frame in judgeLine.RotateFrames) yield return frame.ToString(index, "cd");
            foreach (var frame in judgeLine.AlphaFrames) yield return frame.ToString(index, "ca");
            // Event
            foreach (var ev in judgeLine.MoveEvents) yield return ev.ToString(index);
            foreach (var ev in judgeLine.RotateEvents) yield return ev.ToString(index, "cr");
            foreach (var ev in judgeLine.AlphaEvents) yield return ev.ToString(index, "cf");
            // Note
            foreach (var note in judgeLine.NoteList) yield return note.ToString(index);
        }

        /// <summary>
        /// 枚举所有导出行
        /// </summary>
        private IEnumerable<string> GetExportLines()
        {
            yield return Offset.ToString();
            foreach (var bpm in BpmList)
                yield return bpm.ToString();
            for (var i = 0; i < JudgeLineList.Count; i++)
                foreach (var line in GetJudgeLineLines(JudgeLineList[i], i))
                    yield return line;
        }

        /// <summary>
        /// 导出PhiEditChart
        /// </summary>
        /// <returns>PhiEditChart</returns>
        [PublicAPI]
        public string Export() => string.Join(Environment.NewLine, GetExportLines());

        /// <summary>
        /// 异步导出PhiEditChart
        /// </summary>
        /// <returns>PhiEditChart</returns>
        public async Task<string> ExportAsync()
            => await Task.Run(Export);

        /// <summary>
        /// 流式导出PhiEditChart
        /// </summary>
        /// <param name="stream"></param>
        public void ExportToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            foreach (var line in GetExportLines())
                writer.WriteLine(line);
        }

        /// <summary>
        /// 流式导出PhiEditChart
        /// </summary>
        /// <param name="stream"></param>
        public async Task ExportToStreamAsync(Stream stream)
        {
            await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            foreach (var line in GetExportLines())
                await writer.WriteLineAsync(line);
        }
    }
}