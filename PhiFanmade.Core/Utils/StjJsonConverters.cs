using System;
using PhiFanmade.Core.Common;
#if !NETSTANDARD2_1
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace PhiFanmade.Core.Utils
{
#if !NETSTANDARD2_1
    // 注意：为避免与现有 Json.NET 转换器同名冲突，STJ 版本统一使用 Stj* 命名
    public class StjBeatJsonConverter : JsonConverter<Beat>
    {
        public override Beat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 手动读取数组，避免 AOT 问题
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of array for Beat");

            reader.Read();
            var values = new int[3];
            for (int i = 0; i < 3 && reader.TokenType == JsonTokenType.Number; i++)
            {
                values[i] = reader.GetInt32();
                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected end of array for Beat");

            return new Beat(values);
        }

        public override void Write(Utf8JsonWriter writer, Beat value, JsonSerializerOptions options)
        {
            // 手动写入数组，避免 AOT 问题
            int[] beatArray = value;
            writer.WriteStartArray();
            foreach (var v in beatArray)
            {
                writer.WriteNumberValue(v);
            }

            writer.WriteEndArray();
        }
    }
#endif
}