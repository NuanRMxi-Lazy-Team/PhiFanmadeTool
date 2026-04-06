using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiChain.v6.JsonConverter
{
    public sealed class NoteJsonConverter : JsonConverter<Note>
    {
        public override void WriteJson(JsonWriter writer, Note value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                ["kind"] = ToKindString(value.Kind),
                ["above"] = value.Above,
                ["beat"] = JToken.FromObject(value.Beat, serializer),
                ["x"] = value.X,
                ["speed"] = value.Speed
            };

            if (value.Kind == NoteKind.Hold)
            {
                obj["hold_beat"] = JToken.FromObject(value.HoldBeat, serializer);
            }

            obj.WriteTo(writer);
        }

        public override Note ReadJson(JsonReader reader, Type objectType, Note existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var note = existingValue ?? new Note();

            note.Kind = ParseKind(obj.Value<string>("kind"));
            note.Above = obj.Value<bool?>("above") ?? false;
            note.Beat = obj["beat"]?.ToObject<Beat>(serializer) ?? new Beat(new[] { 0, 0, 1 });
            note.X = obj.Value<float?>("x") ?? 0f;
            note.Speed = obj.Value<float?>("speed") ?? 1f;

            if (note.Kind == NoteKind.Hold)
            {
                note.HoldBeat = obj["hold_beat"]?.ToObject<Beat>(serializer) ?? new Beat(new[] { 0, 0, 1 });
            }
            else
            {
                note.HoldBeat = new Beat(new[] { 0, 0, 1 });
            }

            return note;
        }

        private static string ToKindString(NoteKind kind)
        {
            switch (kind)
            {
                case NoteKind.Tap:
                    return "tap";
                case NoteKind.Drag:
                    return "drag";
                case NoteKind.Hold:
                    return "hold";
                case NoteKind.Flick:
                    return "flick";
                default:
                    throw new JsonSerializationException("Unknown note kind.");
            }
        }

        private static NoteKind ParseKind(string kind)
        {
            switch (kind)
            {
                case "tap":
                    return NoteKind.Tap;
                case "drag":
                    return NoteKind.Drag;
                case "hold":
                    return NoteKind.Hold;
                case "flick":
                    return NoteKind.Flick;
                default:
                    throw new JsonSerializationException("Unsupported note kind: " + kind);
            }
        }
    }
}