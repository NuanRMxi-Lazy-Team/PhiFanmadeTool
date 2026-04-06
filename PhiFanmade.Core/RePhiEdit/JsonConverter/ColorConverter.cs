using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit.JsonConverter
{
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
}