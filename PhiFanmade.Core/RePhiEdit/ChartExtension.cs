using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace PhiFanmade.Core.RePhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 对判定线及其事件层级进行预处理。
        /// </summary>
        public void Anticipation()
        {
            foreach (var judgeLine in JudgeLineList)
            {
                // 如果这个判定线层级上有null层级，移除它们
                judgeLine.EventLayers.RemoveAll(layer => layer == null);
                // 对所有判定线的所有事件层级执行Anticipation()方法
                foreach (var eventLayer in judgeLine.EventLayers)
                {
                    eventLayer.Anticipation();
                    eventLayer.Sort();
                }

                judgeLine.Extended.Anticipation();
                // 如果判定线上有任何类型的Control组为空或null，则设定一个默认值
                if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                    judgeLine.AlphaControls = AlphaControl.Default;
                if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                    judgeLine.PositionControls = XControl.Default;
                if (judgeLine.SizeControls == null || judgeLine.SizeControls.Count == 0)
                    judgeLine.SizeControls = SizeControl.Default;
                if (judgeLine.SkewControls == null || judgeLine.SkewControls.Count == 0)
                    judgeLine.SkewControls = SkewControl.Default;
                if (judgeLine.YControls == null || judgeLine.YControls.Count == 0)
                    judgeLine.YControls = YControl.Default;
            }
        }

        /// <summary>
        /// 序列化谱面
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>Json</returns>
        public string ExportToJson(bool format)
        {
            Anticipation();
            return JsonConvert.SerializeObject(this, format ? Formatting.Indented : Formatting.None);
        }

        public void ExportToJsonStream(Stream stream, bool format)
        {
            Anticipation();
            using var streamWriter = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
            var serializer = new JsonSerializer
            {
                Formatting = format ? Formatting.Indented : Formatting.None
            };

            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, this);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        public async Task ExportToJsonStreamAsync(Stream stream, bool format)
        {
            Anticipation();
            await using var streamWriter =
                new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
            var serializer = new JsonSerializer
            {
                Formatting = format ? Formatting.Indented : Formatting.None
            };

            await Task.Run(() =>
            {
                using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
                serializer.Serialize(jsonWriter, this);
                jsonWriter.Flush();
            });

            await streamWriter.FlushAsync();
        }


        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
        public static Chart LoadFromJson(string json)
        {
            var chart = JsonConvert.DeserializeObject<Chart>(json) ??
                        throw new InvalidOperationException(
                            "Failed to deserialize Chart from JSON.");
            foreach (var judgeLine in chart.JudgeLineList)
            {
                // 如果这个判定线层级上有null层级，移除它们
                judgeLine.EventLayers.RemoveAll(layer => layer == null);
                foreach (var eventLayer in judgeLine.EventLayers)
                    eventLayer.Sort();
            }

            return chart;
        }

        /// <summary>
        /// 异步序列化为Json
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>json</returns>
        public Task<string> ExportToJsonAsync(bool format)
            => Task.Run(() => ExportToJson(format));


        /// <summary>
        /// 异步从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        public static Task<Chart> LoadFromJsonAsync(string json)
            => Task.Run(() => LoadFromJson(json));

        public Chart Clone()
        {
            return new Chart
            {
                BpmList = BpmList.ConvertAll(bpm => bpm.Clone()),
                Meta = Meta.Clone(),
                JudgeLineList = JudgeLineList.ConvertAll(judgeLine => judgeLine.Clone()),
                ChartTime = ChartTime,
                JudgeLineGroup = JudgeLineGroup.ToArray(),
                MultiLineString = MultiLineString,
                MultiScale = MultiScale,
                XyBind = XyBind
            };
        }
    }
}