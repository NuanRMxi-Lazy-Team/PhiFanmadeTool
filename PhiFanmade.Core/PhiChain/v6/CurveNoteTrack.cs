using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiChain.v6
{
    public sealed class CurveNoteTrack
    {
        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("to")]
        public int To { get; set; }

        [JsonProperty("kind")]
        public NoteKind Kind { get; set; } = NoteKind.Drag;

        [JsonProperty("hold_beat", NullValueHandling = NullValueHandling.Ignore)]
        public Beat HoldBeat { get; set; }

        [JsonProperty("density")]
        public uint Density { get; set; } = 16;

        [JsonProperty("curve")]
        public Easing Curve { get; set; } = Easing.Linear;
    }
}

