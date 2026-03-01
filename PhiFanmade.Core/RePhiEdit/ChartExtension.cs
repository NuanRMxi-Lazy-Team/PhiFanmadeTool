using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
#if !NETSTANDARD2_1
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

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

        /// <summary>
        /// 流式序列化铺面
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="format"></param>
        public async Task ExportToJsonStreamAsync(Stream stream, bool format)
        {
            Anticipation();
            await using var streamWriter =
                new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
            // .NETStandard 2.1不允许await using，所以这里不使用await using
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = new Newtonsoft.Json.JsonSerializer
            {
                Formatting = format ? Formatting.Indented : Formatting.None
            };

            await Task.Run(() => serializer.Serialize(jsonWriter, this));
            await jsonWriter.FlushAsync();
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

#if !NETSTANDARD2_1
        /// <summary>
        /// 使用 System.Text.Json 序列化谱面
        /// </summary>
        public string ExportToJsonStj(bool format)
        {
            Anticipation();

            var options = new JsonSerializerOptions
            {
                WriteIndented = format,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 保持中文等非 ASCII 字符不转义
            };
            options.TypeInfoResolver = RePhiEditJsonContext.Default;

            return System.Text.Json.JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// 使用 System.Text.Json 流式序列化到谱面
        /// </summary>
        public void ExportToJsonStjStream(Stream stream, bool format)
        {
            Anticipation();

            var options = new JsonSerializerOptions
            {
                WriteIndented = format,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = RePhiEditJsonContext.Default
            };

            System.Text.Json.JsonSerializer.Serialize(stream, this, options);
            stream.Flush();
        }

        /// <summary>
        /// 使用 System.Text.Json 异步流式序列化到谱面
        /// </summary>
        public async Task ExportToJsonStjStreamAsync(Stream stream, bool format)
        {
            Anticipation();

            var options = new JsonSerializerOptions
            {
                WriteIndented = format,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = RePhiEditJsonContext.Default
            };

            await System.Text.Json.JsonSerializer.SerializeAsync(stream, this, options);
            await stream.FlushAsync();
        }

        /// <summary>
        /// 使用 System.Text.Json 从Json反序列化
        /// </summary>
        public static Chart LoadFromJsonStj(string json)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                TypeInfoResolver = RePhiEditJsonContext.Default
            };
            var chart = System.Text.Json.JsonSerializer.Deserialize<Chart>(json, options) ??
                        throw new InvalidOperationException("Failed to deserialize Chart from JSON.");
            foreach (var judgeLine in chart.JudgeLineList)
            {
                judgeLine.EventLayers.RemoveAll(layer => layer == null);
                foreach (var eventLayer in judgeLine.EventLayers)
                    eventLayer.Sort();
            }

            return chart;
        }

        public Task<string> ExportToJsonStjAsync(bool format)
            => Task.Run(() => ExportToJsonStj(format));

        public static Task<Chart> LoadFromJsonStjAsync(string json)
            => Task.Run(() => LoadFromJsonStj(json));
#endif

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