using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit.JsonConverter
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
}