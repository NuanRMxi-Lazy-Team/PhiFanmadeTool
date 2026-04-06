using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.RePhiEdit.JsonConverter
{
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

        public override List<Event<byte[]>> ReadJson(JsonReader reader, Type objectType,
            List<Event<byte[]>> existingValue, bool hasExistingValue, JsonSerializer serializer)
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
}