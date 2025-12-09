using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
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

    /// <summary>
    /// 用于兼容多字段名的颜色数组转换器
    /// </summary>
    public class ColorConverter : JsonConverter<byte[]>
    {
        public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
        {
            writer.WritePropertyName("tint");
            writer.WriteStartArray();
            foreach (var v in value)
            {
                writer.WriteValue(v);
            }

            writer.WriteEndArray();
        }

        public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            // 读取字段名（color 或 tint）
            string propertyName = reader.TokenType == JsonToken.PropertyName
                ? (reader.Value?.ToString() ?? "").ToLower()
                : "";

            // 前进到下一个 token
            reader.Read();

            // 当字段是 tint 或 color 且内容是数组时读取
            if ((propertyName == "tint" || propertyName == "color") &&
                reader.TokenType == JsonToken.StartArray)
            {
                var list = serializer.Deserialize<List<byte>>(reader);
                return list?.ToArray() ?? existingValue;
            }

            return existingValue;
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
}