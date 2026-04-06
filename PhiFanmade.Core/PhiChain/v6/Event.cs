using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PhiFanmade.Core.Common;
using PhiFanmade.Core.PhiChain.v6.JsonConverter;

namespace PhiFanmade.Core.PhiChain.v6
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LineEventKind
    {
        [EnumMember(Value = "x")]
        X,

        [EnumMember(Value = "y")]
        Y,

        [EnumMember(Value = "rotation")]
        Rotation,

        [EnumMember(Value = "opacity")]
        Opacity,

        [EnumMember(Value = "speed")]
        Speed
    }

    public enum LineEventValueKind
    {
        Transition,
        Constant
    }

    [JsonConverter(typeof(LineEventValueJsonConverter))]
    public sealed class LineEventValue
    {
        [JsonIgnore]
        public LineEventValueKind Kind { get; set; } = LineEventValueKind.Constant;

        [JsonIgnore]
        public float Start { get; set; }

        [JsonIgnore]
        public float End { get; set; }

        [JsonIgnore]
        public Easing Easing { get; set; } = Easing.Linear;

        [JsonIgnore]
        public float Value { get; set; }

        public static LineEventValue Transition(float start, float end, Easing easing)
        {
            return new LineEventValue
            {
                Kind = LineEventValueKind.Transition,
                Start = start,
                End = end,
                Easing = easing ?? Easing.Linear
            };
        }

        public static LineEventValue Constant(float value)
        {
            return new LineEventValue
            {
                Kind = LineEventValueKind.Constant,
                Value = value
            };
        }
    }

    public sealed class LineEvent
    {
        [JsonProperty("kind")]
        public LineEventKind Kind { get; set; }

        [JsonProperty("start_beat")]
        public Beat StartBeat { get; set; } = new Beat(new[] { 0, 0, 1 });

        [JsonProperty("end_beat")]
        public Beat EndBeat { get; set; } = new Beat(new[] { 1, 0, 1 });

        [JsonProperty("value")]
        public LineEventValue Value { get; set; } = LineEventValue.Constant(0f);
    }
}

