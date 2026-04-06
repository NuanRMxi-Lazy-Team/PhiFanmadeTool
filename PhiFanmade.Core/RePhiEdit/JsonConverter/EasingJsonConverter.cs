using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit.JsonConverter
{
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