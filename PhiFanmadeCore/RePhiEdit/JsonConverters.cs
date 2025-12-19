using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        /// <summary>
        /// 用于将cmdysj在JSON中表示为0和1的布尔值进行转换
        /// </summary>
        public class BoolConverter : JsonConverter<bool>
        {
            public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
            {
                writer.WriteValue(value ? 1 : 0);
            }

            public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value is long longValue)
                {
                    return longValue == 1;
                }

                if (reader.Value is int intValue)
                {
                    return intValue == 1;
                }

                return existingValue;
            }
        }

        public class ColorConverter : JsonConverter<byte[]>
        {
            public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                foreach (var b in value)
                {
                    writer.WriteValue(b);
                }

                writer.WriteEndArray();
            }

            public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    var list = new List<byte>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndArray)
                            break;

                        if (reader.TokenType == JsonToken.Integer)
                        {
                            list.Add(Convert.ToByte(reader.Value));
                        }
                    }

                    return list.ToArray();
                }

                return existingValue ?? new byte[] { 255, 255, 255 };
            }
        }


        /// <summary>
        /// 用于转换音符类型枚举的转换器
        /// </summary>
        public class NoteTypeConverter : JsonConverter<NoteType>
        {
            public override void WriteJson(JsonWriter writer, NoteType value, JsonSerializer serializer)
            {
                writer.WriteValue((int)value);
            }

            public override NoteType ReadJson(JsonReader reader, Type objectType, NoteType existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value is long longValue)
                {
                    return (NoteType)longValue;
                }

                if (reader.Value is int intValue)
                {
                    return (NoteType)intValue;
                }

                return existingValue;
            }
        }

        public class AttachUiConverter : JsonConverter<AttachUi?>
        {
            public override void WriteJson(JsonWriter writer, AttachUi? value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                switch (value)
                {
                    case AttachUi.Bar
                        :
                        writer.WriteValue("bar");
                        break;
                    case AttachUi.Combo
                        :
                        writer.WriteValue("combo");
                        break;
                    case AttachUi.ComboNumber
                        :
                        writer.WriteValue("combonumber");
                        break;
                    case AttachUi.Level
                        :
                        writer.WriteValue("level");
                        break;
                    case AttachUi.Name
                        :
                        writer.WriteValue("name");
                        break;
                    case AttachUi.Pause
                        :
                        writer.WriteValue("pause");
                        break;
                    case AttachUi.Score
                        :
                        writer.WriteValue("score");
                        break;
                }
            }

            public override AttachUi? ReadJson(JsonReader reader, Type objectType, AttachUi? existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                else if (reader.TokenType == JsonToken.String)
                {
                    string value = (string)reader.Value;
                    var lowerValue = value.ToLower();
                    switch (lowerValue)
                    {
                        case "bar": return AttachUi.Bar;
                        case "combo": return AttachUi.Combo;
                        case "combonumber": return AttachUi.ComboNumber;
                        case "level": return AttachUi.Level;
                        case "name": return AttachUi.Name;
                        case "pause": return AttachUi.Pause;
                        case "score": return AttachUi.Score;
                        default: return existingValue;
                    }
                }

                return existingValue;
            }
        }

        public class ColorEventsConverter : JsonConverter<List<Event<byte[]>>>
        {
            public override void WriteJson(JsonWriter writer, List<Event<byte[]>> value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                foreach (var evt in value)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("start");
                    writer.WriteStartArray();
                    foreach (var b in evt.StartValue)
                        writer.WriteValue((int)b);
                    writer.WriteEndArray();

                    writer.WritePropertyName("end");
                    writer.WriteStartArray();
                    foreach (var b in evt.EndValue)
                        writer.WriteValue((int)b);
                    writer.WriteEndArray();
                    
                    writer.WritePropertyName("startTime");
                    writer.WriteStartArray();
                    foreach (var b in (int[])evt.StartBeat)
                        writer.WriteValue(b);
                    writer.WriteEndArray();
                    writer.WritePropertyName("endTime");
                    writer.WriteStartArray();
                    foreach (var b in (int[])evt.EndBeat)
                        writer.WriteValue(b);
                    writer.WriteEndArray();
                    writer.WritePropertyName("easingType");
                    writer.WriteValue((int)evt.Easing);
                    writer.WritePropertyName("easingLeft");
                    writer.WriteValue(evt.EasingLeft);
                    writer.WritePropertyName("easingRight");
                    writer.WriteValue(evt.EasingRight);
                    writer.WritePropertyName("bezier");
                    writer.WriteValue(evt.IsBezier ? 1 : 0);
                    writer.WritePropertyName("bezierPoints");
                    writer.WriteStartArray();
                    foreach (var f in evt.BezierPoints)
                        writer.WriteValue(f);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            public override List<Event<byte[]>> ReadJson(JsonReader reader, Type objectType, List<Event<byte[]>> existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var result = new List<Event<byte[]>>();
                var token = Newtonsoft.Json.Linq.JToken.Load(reader);

                foreach (var item in token)
                {
                    var evt = new Event<byte[]>
                    {
                        StartBeat = new Beat(item["startTime"]?.ToObject<int[]>()),
                        EndBeat = new Beat(item["endTime"]?.ToObject<int[]>()),
                        StartValue = Array.ConvertAll(item["start"]?.ToObject<int[]>() ?? Array.Empty<int>(), i => (byte)i),
                        EndValue = Array.ConvertAll(item["end"]?.ToObject<int[]>() ?? Array.Empty<int>(), i => (byte)i),
                        Easing = new Easing(item["easingType"]?.ToObject<int>() ?? 1),
                        EasingLeft = item["easingLeft"]?.ToObject<float>() ?? 0.0f,
                        EasingRight = item["easingRight"]?.ToObject<float>() ?? 1.0f,
                        IsBezier = item["bezier"]?.ToObject<int>() == 1,
                        BezierPoints = item["bezierPoints"]?.ToObject<float[]>() ?? new float[4]
                    };
                    result.Add(evt);
                }

                return result;
            }
        }
        
        public class BeatJsonConverter : JsonConverter<Beat>
        {
            public override void WriteJson(JsonWriter writer, Beat value, JsonSerializer serializer)
            {
                // 将 Beat 序列化为 int[] 数组
                int[] beatArray = value;
                serializer.Serialize(writer, beatArray);
            }

            public override Beat ReadJson(JsonReader reader, Type objectType, Beat existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                // 从 int[] 数组反序列化为 Beat
                var beatArray = serializer.Deserialize<int[]>(reader);
                return new Beat(beatArray);
            }
        }
        
        public class EasingJsonConverter : JsonConverter<Easing>
        {
            public override void WriteJson(JsonWriter writer, Easing value, JsonSerializer serializer)
            {
                writer.WriteValue((int)value);
            }

            public override Easing ReadJson(JsonReader reader, Type objectType, Easing existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                var easingNumber = serializer.Deserialize<int>(reader);
                return new Easing(easingNumber);
            }
        }
    }
}