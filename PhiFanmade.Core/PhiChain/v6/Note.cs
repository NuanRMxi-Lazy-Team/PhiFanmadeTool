using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PhiFanmade.Core.Common;
using PhiFanmade.Core.PhiChain.v6.JsonConverter;

namespace PhiFanmade.Core.PhiChain.v6
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NoteKind
    {
        [EnumMember(Value = "tap")]
        Tap,

        [EnumMember(Value = "drag")]
        Drag,

        [EnumMember(Value = "hold")]
        Hold,

        [EnumMember(Value = "flick")]
        Flick
    }

    [JsonConverter(typeof(NoteJsonConverter))]
    public sealed class Note
    {
        [JsonIgnore]
        public NoteKind Kind { get; set; } = NoteKind.Tap;

        [JsonIgnore]
        public Beat HoldBeat { get; set; } = new Beat(new[] { 0, 0, 1 });

        [JsonProperty("above")]
        public bool Above { get; set; }

        [JsonProperty("beat")]
        public Beat Beat { get; set; } = new Beat(new[] { 0, 0, 1 });

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; } = 1f;
    }
}


