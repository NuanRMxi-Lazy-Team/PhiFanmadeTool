using System;
using PhiFanmade.Core.Common;
using Newtonsoft.Json;

namespace PhiFanmade.Core.Utils
{
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
}