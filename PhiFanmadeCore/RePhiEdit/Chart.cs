using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
// STJ 支持
#if !NETSTANDARD2_1
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class Chart
        {
            /// <summary>
            /// 序列化为Json
            /// </summary>
            /// <param name="format">是否需要格式化</param>
            /// <returns>Json</returns>
            public string ExportToJson(bool format)
            {
                foreach (var judgeLine in JudgeLineList)
                {
                    // 如果这个判定线层级上有null层级，移除它们
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    // 对所有判定线的所有事件层级执行Anticipation()方法
                    foreach (var eventlayer in judgeLine.EventLayers)
                    {
                        eventlayer.Anticipation();
                        eventlayer.Sort();
                    }

                    judgeLine.Extended.Anticipation();
                    // 如果判定线上有任何类型的Control组为空或null，则设定一个默认值
                    if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                        judgeLine.AlphaControls = AlphaControl.Default;
                    if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                        judgeLine.PositionControls = XControl.Default;
                }

                return JsonConvert.SerializeObject(this, format ? Formatting.Indented : Formatting.None);
            }

            public async Task ExportToJsonStreamAsync(System.IO.Stream stream, bool format)
            {
                foreach (var judgeLine in JudgeLineList)
                {
                    // 如果这个判定线层级上有null层级，移除它们
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    // 对所有判定线的所有事件层级执行Anticipation()方法
                    foreach (var eventlayer in judgeLine.EventLayers)
                    {
                        eventlayer.Anticipation();
                        eventlayer.Sort();
                    }

                    judgeLine.Extended.Anticipation();
                    // 如果判定线上有任何类型的Control组为空或null，则设定一个默认值
                    if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                        judgeLine.AlphaControls = AlphaControl.Default;
                    if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                        judgeLine.PositionControls = XControl.Default;
                }

                await using var streamWriter =
                    new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);
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
                    foreach (var eventlayer in judgeLine.EventLayers)
                        eventlayer.Sort();
                }

                return chart;
            }

#if !NETSTANDARD2_1
            /// <summary>
            /// 使用 System.Text.Json 序列化为Json
            /// </summary>
            public string ExportToJsonStj(bool format)
            {
                foreach (var judgeLine in JudgeLineList)
                {
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    foreach (var eventlayer in judgeLine.EventLayers)
                    {
                        eventlayer.Anticipation();
                        eventlayer.Sort();
                    }

                    judgeLine.Extended.Anticipation();
                    if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                        judgeLine.AlphaControls = AlphaControl.Default;
                    if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                        judgeLine.PositionControls = XControl.Default;
                }

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
            /// 使用 System.Text.Json 流式序列化到流（防止OOM）
            /// </summary>
            public void ExportToJsonStjStream(System.IO.Stream stream, bool format)
            {
                foreach (var judgeLine in JudgeLineList)
                {
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    foreach (var eventlayer in judgeLine.EventLayers)
                    {
                        eventlayer.Anticipation();
                        eventlayer.Sort();
                    }

                    judgeLine.Extended.Anticipation();
                    if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                        judgeLine.AlphaControls = AlphaControl.Default;
                    if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                        judgeLine.PositionControls = XControl.Default;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = format,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    IncludeFields = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                options.TypeInfoResolver = RePhiEditJsonContext.Default;

                System.Text.Json.JsonSerializer.Serialize(stream, this, options);
                stream.Flush();
            }

            /// <summary>
            /// 使用 System.Text.Json 异步流式序列化到流（防止OOM）
            /// </summary>
            public async Task ExportToJsonStjStreamAsync(System.IO.Stream stream, bool format)
            {
                foreach (var judgeLine in JudgeLineList)
                {
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    foreach (var eventlayer in judgeLine.EventLayers)
                    {
                        eventlayer.Anticipation();
                        eventlayer.Sort();
                    }

                    judgeLine.Extended.Anticipation();
                    if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                        judgeLine.AlphaControls = AlphaControl.Default;
                    if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                        judgeLine.PositionControls = XControl.Default;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = format,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    IncludeFields = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                options.TypeInfoResolver = RePhiEditJsonContext.Default;

                await System.Text.Json.JsonSerializer.SerializeAsync(stream, this, typeof(Chart), options);
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
                    IncludeFields = true
                };
                options.TypeInfoResolver = RePhiEditJsonContext.Default;
                var chart = System.Text.Json.JsonSerializer.Deserialize<Chart>(json, options) ??
                            throw new InvalidOperationException("Failed to deserialize Chart from JSON.");
                foreach (var judgeLine in chart.JudgeLineList)
                {
                    judgeLine.EventLayers.RemoveAll(layer => layer == null);
                    foreach (var eventlayer in judgeLine.EventLayers)
                        eventlayer.Sort();
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
                return new Chart()
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


            /// <summary>
            /// 坐标系边界
            /// </summary>
            public static class CoordinateSystem
            {
                public const float MaxX = 675f;
                public const float MinX = -675f;
                public const float MaxY = 450f;
                public const float MinY = -450f;
            }

            /// <summary>
            /// BPM列表
            /// </summary>
            [JsonProperty("BPMList")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("BPMList")]
#endif
            public List<Bpm> BpmList = new List<Bpm>
            {
                new Bpm()
            };

            /// <summary>
            /// 元数据
            /// </summary>
            [JsonProperty("META")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("META")]
#endif
            public Meta Meta = new Meta();

            /// <summary>
            /// 判定线列表
            /// </summary>
            [JsonProperty("judgeLineList")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("judgeLineList")]
#endif
            public List<JudgeLine> JudgeLineList = new List<JudgeLine>();

            /// <summary>
            /// 制谱时长（秒）
            /// </summary>
            [JsonProperty("chartTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("chartTime")]
#endif
            public double ChartTime = 0d;

            /// <summary>
            /// 判定线组
            /// </summary>
            [JsonProperty("judgeLineGroup")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("judgeLineGroup")]
#endif
            public string[] JudgeLineGroup = { "Default" };

            /// <summary>
            /// 多线编辑判定线列表（以空格为分割，或使用x:y选中x~y所有判定线）
            /// </summary>
            [JsonProperty("multiLineString")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("multiLineString")]
#endif
            public string MultiLineString = "1";

            /// <summary>
            /// 多线编辑页面缩放比例
            /// </summary>
            [JsonProperty("multiScale")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("multiScale")]
#endif
            public float MultiScale = 1.0f;

            /// <summary>
            /// XY事件是否一一对应
            /// </summary>
            [JsonProperty("xybind")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("xybind")]
#endif
            public bool XyBind = true;
        }
    }
}