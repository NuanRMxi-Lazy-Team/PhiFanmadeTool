using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhiFanmade.Core.PhiChain.v6.JsonConverter
{
    public sealed class BpmListJsonConverter : JsonConverter<BpmList>
    {
        public override void WriteJson(JsonWriter writer, BpmList value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (IReadOnlyList<BpmPoint>)value);
        }

        public override BpmList ReadJson(JsonReader reader, Type objectType, BpmList existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            var points = array.ToObject<List<BpmPoint>>(serializer) ?? new List<BpmPoint>();
            var list = new BpmList(points);
            list.ComputeTimes();
            return list;
        }
    }
}