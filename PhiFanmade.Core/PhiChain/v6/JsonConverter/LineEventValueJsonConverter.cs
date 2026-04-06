using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhiFanmade.Core.PhiChain.v6.JsonConverter
{
    public sealed class LineEventValueJsonConverter : JsonConverter<LineEventValue>
    {
        public override void WriteJson(JsonWriter writer, LineEventValue value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                ["type"] = value.Kind == LineEventValueKind.Constant ? "constant" : "transition"
            };

            if (value.Kind == LineEventValueKind.Constant)
            {
                obj["value"] = value.Value;
            }
            else
            {
                obj["start"] = value.Start;
                obj["end"] = value.End;
                obj["easing"] = JToken.FromObject(value.Easing, serializer);
            }

            obj.WriteTo(writer);
        }

        public override LineEventValue ReadJson(JsonReader reader, Type objectType, LineEventValue existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var type = obj.Value<string>("type");

            if (type == "constant")
            {
                return LineEventValue.Constant(obj.Value<float?>("value") ?? 0f);
            }

            if (type == "transition")
            {
                return LineEventValue.Transition(
                    obj.Value<float?>("start") ?? 0f,
                    obj.Value<float?>("end") ?? 0f,
                    obj["easing"]?.ToObject<Easing>(serializer) ?? Easing.Linear
                );
            }

            throw new JsonSerializationException("Unsupported line event value type: " + type);
        }
    }
}