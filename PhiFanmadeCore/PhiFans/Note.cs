using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Note
        {
            [JsonProperty("type")] public NoteType Type = NoteType.Tap;
            [JsonProperty("beat")] public Beat Beat = new Beat(0);
            [JsonProperty("positionX")] public float PositionX;
            [JsonProperty("speed")] public float Speed;
            [JsonProperty("isAbove")] public bool IsAbove = true;
            [JsonProperty("holdEndBeat")] public Beat HoldEndBeat = new Beat(0);
        }
        
        public enum NoteType
        {
            Tap = 1,
            Hold = 3,
            Flick = 4,
            Drag = 2
        }
    }
    
}