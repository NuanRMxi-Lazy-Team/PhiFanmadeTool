// AOT: System.Text.Json 源生成上下文（仅在 .NET 8+ 下启用）

#if !NETSTANDARD2_1
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PhiFanmade.Core.RePhiEdit
{
    [JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
    [JsonSerializable(typeof(RePhiEdit.Chart))]
    [JsonSerializable(typeof(RePhiEdit.Bpm))]
    [JsonSerializable(typeof(RePhiEdit.Meta))]
    [JsonSerializable(typeof(RePhiEdit.JudgeLine))]
    [JsonSerializable(typeof(RePhiEdit.EventLayer))]
    [JsonSerializable(typeof(RePhiEdit.ExtendLayer))]
    [JsonSerializable(typeof(RePhiEdit.Note))]
    [JsonSerializable(typeof(RePhiEdit.Easing))]
    [JsonSerializable(typeof(RePhiEdit.Beat))]
    // 基础类型数组
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(float[]))]
    [JsonSerializable(typeof(double[]))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(string[]))]
    // 集合类型
    [JsonSerializable(typeof(List<RePhiEdit.Bpm>))]
    [JsonSerializable(typeof(List<RePhiEdit.JudgeLine>))]
    [JsonSerializable(typeof(List<RePhiEdit.EventLayer>))]
    [JsonSerializable(typeof(List<RePhiEdit.Note>))]
    [JsonSerializable(typeof(List<RePhiEdit.AlphaControl>))]
    [JsonSerializable(typeof(List<RePhiEdit.XControl>))]
    [JsonSerializable(typeof(List<RePhiEdit.YControl>))]
    [JsonSerializable(typeof(List<RePhiEdit.SizeControl>))]
    [JsonSerializable(typeof(List<RePhiEdit.SkewControl>))]
    // Event<T> 的闭包类型及其List
    [JsonSerializable(typeof(RePhiEdit.Event<float>))]
    [JsonSerializable(typeof(RePhiEdit.Event<int>))]
    [JsonSerializable(typeof(RePhiEdit.Event<string>))]
    [JsonSerializable(typeof(RePhiEdit.Event<byte[]>))]
    [JsonSerializable(typeof(List<RePhiEdit.Event<float>>))]
    [JsonSerializable(typeof(List<RePhiEdit.Event<int>>))]
    [JsonSerializable(typeof(List<RePhiEdit.Event<string>>))]
    [JsonSerializable(typeof(List<RePhiEdit.Event<byte[]>>))]
    // Controls
    [JsonSerializable(typeof(RePhiEdit.AlphaControl))]
    [JsonSerializable(typeof(RePhiEdit.XControl))]
    [JsonSerializable(typeof(RePhiEdit.SizeControl))]
    [JsonSerializable(typeof(RePhiEdit.SkewControl))]
    [JsonSerializable(typeof(RePhiEdit.YControl))]
    public partial class RePhiEditJsonContext : JsonSerializerContext
    {
    }
}
#endif