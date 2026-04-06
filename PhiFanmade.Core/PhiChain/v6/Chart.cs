using System.Collections.Generic;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiChain.v6
{
    public sealed partial class Chart
    {
        [JsonProperty("format")] public ulong Format { get; set; } = Constants.CurrentFormat;

        [JsonProperty("offset")] public float Offset { get; set; }

        [JsonProperty("bpm_list")] public BpmList BpmList { get; set; } = new();

        [JsonProperty("lines")]
        public List<SerializedLine> Lines { get; set; } = new() { SerializedLine.CreateDefault() };

        public static Chart Empty()
        {
            return new Chart
            {
                Lines = new List<SerializedLine>()
            };
        }
    }

    public sealed class SerializedLine
    {
        // Rust uses serde(flatten) for line; v6 currently only contains name.
        [JsonProperty("name")] public string Name { get; set; } = "Unnamed Line";

        [JsonIgnore]
        public Line Line
        {
            get => new() { Name = Name };
            set => Name = value?.Name ?? "Unnamed Line";
        }

        [JsonProperty("notes")] public List<Note> Notes { get; set; } = new();

        [JsonProperty("events")] public List<LineEvent> Events { get; set; } = DefaultEvents();

        [JsonProperty("children")] public List<SerializedLine> Children { get; set; } = new();

        [JsonProperty("curve_note_tracks")] public List<CurveNoteTrack> CurveNoteTracks { get; set; } = new();

        public static SerializedLine CreateDefault()
        {
            return new SerializedLine();
        }

        private static List<LineEvent> DefaultEvents()
        {
            return new List<LineEvent>
            {
                NewConstEvent(LineEventKind.X, 0f),
                NewConstEvent(LineEventKind.Y, 0f),
                NewConstEvent(LineEventKind.Rotation, 0f),
                NewConstEvent(LineEventKind.Opacity, 0f),
                NewConstEvent(LineEventKind.Speed, 10f)
            };
        }

        private static LineEvent NewConstEvent(LineEventKind kind, float value)
        {
            return new LineEvent
            {
                Kind = kind,
                Value = LineEventValue.Constant(value),
                StartBeat = new Beat(new[] { 0, 0, 1 }),
                EndBeat = new Beat(new[] { 1, 0, 1 })
            };
        }
    }
}