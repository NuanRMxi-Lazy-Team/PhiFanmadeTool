// AOT: System.Text.Json 源生成上下文（仅在 .NET 8+ 下启用）

#if !NETSTANDARD2_1
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.RePhiEdit
{
    [JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
    [JsonSerializable(typeof(Chart))]
    [JsonSerializable(typeof(Bpm))]
    [JsonSerializable(typeof(Meta))]
    [JsonSerializable(typeof(JudgeLine))]
    [JsonSerializable(typeof(EventLayer))]
    [JsonSerializable(typeof(ExtendLayer))]
    [JsonSerializable(typeof(Note))]
    [JsonSerializable(typeof(Easing))]
    [JsonSerializable(typeof(Beat))]
    // 基础类型数组
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(float[]))]
    [JsonSerializable(typeof(double[]))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(string[]))]
    // 集合类型
    [JsonSerializable(typeof(List<Bpm>))]
    [JsonSerializable(typeof(List<JudgeLine>))]
    [JsonSerializable(typeof(List<EventLayer>))]
    [JsonSerializable(typeof(List<Note>))]
    [JsonSerializable(typeof(List<AlphaControl>))]
    [JsonSerializable(typeof(List<XControl>))]
    [JsonSerializable(typeof(List<YControl>))]
    [JsonSerializable(typeof(List<SizeControl>))]
    [JsonSerializable(typeof(List<SkewControl>))]
    // Event<T> 的闭包类型及其List
    [JsonSerializable(typeof(Event<float>))]
    [JsonSerializable(typeof(Event<int>))]
    [JsonSerializable(typeof(Event<string>))]
    [JsonSerializable(typeof(Event<byte[]>))]
    [JsonSerializable(typeof(List<Event<float>>))]
    [JsonSerializable(typeof(List<Event<int>>))]
    [JsonSerializable(typeof(List<Event<string>>))]
    [JsonSerializable(typeof(List<Event<byte[]>>))]
    // Controls
    [JsonSerializable(typeof(AlphaControl))]
    [JsonSerializable(typeof(XControl))]
    [JsonSerializable(typeof(SizeControl))]
    [JsonSerializable(typeof(SkewControl))]
    [JsonSerializable(typeof(YControl))]
    public partial class RePhiEditJsonContext : JsonSerializerContext
    {
    }
}
#endif