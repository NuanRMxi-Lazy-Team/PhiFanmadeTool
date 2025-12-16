using System;
using System.Collections.Generic;
#if !NETSTANDARD2_1
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace PhiFanmade.Core.RePhiEdit
{
#if !NETSTANDARD2_1
    public static partial class RePhiEdit
    {
        // 注意：为避免与现有 Json.NET 转换器同名冲突，STJ 版本统一使用 Stj* 命名

        public class StjBoolConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.TryGetInt64(out var l) ? l == 1 : reader.GetInt32() == 1,
                    JsonTokenType.True => true,
                    JsonTokenType.False => false,
                    _ => false
                };
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value ? 1 : 0);
            }
        }

        public class StjColorConverter : JsonConverter<byte[]>
        {
            public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    return new byte[] { 255, 255, 255 };
                }

                var list = new List<byte>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt32(out var val))
                        {
                            list.Add((byte)val);
                        }
                    }
                }

                return list.ToArray();
            }

            public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                foreach (var b in value)
                {
                    writer.WriteNumberValue(b);
                }
                writer.WriteEndArray();
            }
        }

        public class StjNoteTypeConverter : JsonConverter<NoteType>
        {
            public override NoteType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt64(out var l)) return (NoteType)l;
                    return (NoteType)reader.GetInt32();
                }
                return default;
            }

            public override void Write(Utf8JsonWriter writer, NoteType value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue((int)value);
            }
        }

        public class StjAttachUiConverter : JsonConverter<AttachUi?>
        {
            public override AttachUi? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                if (reader.TokenType == JsonTokenType.String)
                {
                    var lowerValue = reader.GetString()?.ToLower();
                    return lowerValue switch
                    {
                        "bar" => AttachUi.Bar,
                        "combo" => AttachUi.Combo,
                        "combonumber" => AttachUi.ComboNumber,
                        "level" => AttachUi.Level,
                        "name" => AttachUi.Name,
                        "pause" => AttachUi.Pause,
                        "score" => AttachUi.Score,
                        _ => null
                    };
                }
                return null;
            }

            public override void Write(Utf8JsonWriter writer, AttachUi? value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }
                var str = value switch
                {
                    AttachUi.Bar => "bar",
                    AttachUi.Combo => "combo",
                    AttachUi.ComboNumber => "combonumber",
                    AttachUi.Level => "level",
                    AttachUi.Name => "name",
                    AttachUi.Pause => "pause",
                    AttachUi.Score => "score",
                    _ => null
                };
                if (str is null) writer.WriteNullValue(); else writer.WriteStringValue(str);
            }
        }

        public class StjColorEventsConverter : JsonConverter<List<Event<byte[]>>>
        {
            public override List<Event<byte[]>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var result = new List<Event<byte[]>>();
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    return result;
                }
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var evt = new Event<byte[]>();
                        if (item.TryGetProperty("startTime", out var startTimeEl))
                            evt.StartBeat = new Beat(JsonSerializer.Deserialize<int[]>(startTimeEl.GetRawText(), options));
                        if (item.TryGetProperty("endTime", out var endTimeEl))
                            evt.EndBeat = new Beat(JsonSerializer.Deserialize<int[]>(endTimeEl.GetRawText(), options));
                        if (item.TryGetProperty("start", out var startEl))
                        {
                            var ints = JsonSerializer.Deserialize<int[]>(startEl.GetRawText(), options) ?? Array.Empty<int>();
                            evt.StartValue = Array.ConvertAll(ints, i => (byte)i);
                        }
                        if (item.TryGetProperty("end", out var endEl))
                        {
                            var ints = JsonSerializer.Deserialize<int[]>(endEl.GetRawText(), options) ?? Array.Empty<int>();
                            evt.EndValue = Array.ConvertAll(ints, i => (byte)i);
                        }
                        if (item.TryGetProperty("easingType", out var easingTypeEl))
                            evt.Easing = new Easing(easingTypeEl.GetInt32());
                        if (item.TryGetProperty("easingLeft", out var easingLeftEl))
                            evt.EasingLeft = (float)easingLeftEl.GetDouble();
                        if (item.TryGetProperty("easingRight", out var easingRightEl))
                            evt.EasingRight = (float)easingRightEl.GetDouble();
                        if (item.TryGetProperty("bezier", out var bezierEl))
                            evt.IsBezier = bezierEl.ValueKind == JsonValueKind.Number ? bezierEl.GetInt32() == 1 : bezierEl.GetBoolean();
                        if (item.TryGetProperty("bezierPoints", out var bezierPointsEl))
                            evt.BezierPoints = JsonSerializer.Deserialize<float[]>(bezierPointsEl.GetRawText(), options) ?? new float[4];

                        result.Add(evt);
                    }
                }
                return result;
            }

            public override void Write(Utf8JsonWriter writer, List<Event<byte[]>> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                foreach (var evt in value)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("start");
                    writer.WriteStartArray();
                    foreach (var b in evt.StartValue)
                        writer.WriteNumberValue((int)b);
                    writer.WriteEndArray();

                    writer.WritePropertyName("end");
                    writer.WriteStartArray();
                    foreach (var b in evt.EndValue)
                        writer.WriteNumberValue((int)b);
                    writer.WriteEndArray();

                    writer.WritePropertyName("startTime");
                    writer.WriteStartArray();
                    foreach (var b in (int[])evt.StartBeat)
                        writer.WriteNumberValue(b);
                    writer.WriteEndArray();

                    writer.WritePropertyName("endTime");
                    writer.WriteStartArray();
                    foreach (var b in (int[])evt.EndBeat)
                        writer.WriteNumberValue(b);
                    writer.WriteEndArray();

                    writer.WritePropertyName("easingType");
                    writer.WriteNumberValue((int)evt.Easing);
                    writer.WritePropertyName("easingLeft");
                    writer.WriteNumberValue(evt.EasingLeft);
                    writer.WritePropertyName("easingRight");
                    writer.WriteNumberValue(evt.EasingRight);
                    writer.WritePropertyName("bezier");
                    writer.WriteNumberValue(evt.IsBezier ? 1 : 0);
                    writer.WritePropertyName("bezierPoints");
                    writer.WriteStartArray();
                    foreach (var f in evt.BezierPoints)
                        writer.WriteNumberValue(f);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
        }

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

        public class StjEasingJsonConverter : JsonConverter<Easing>
        {
            public override Easing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var num = reader.GetInt32();
                return new Easing(num);
            }

            public override void Write(Utf8JsonWriter writer, Easing value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue((int)value);
            }
        }
    }
#endif
}
