using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit.JsonConverter
{
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
                return (NoteType)longValue;
            if (reader.Value is int intValue)
                return (NoteType)intValue;
            return existingValue;
        }
    }
}