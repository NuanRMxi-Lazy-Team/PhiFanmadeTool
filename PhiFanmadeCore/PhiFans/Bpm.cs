using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Bpm
        {
            [JsonProperty("beat")]
            public Beat StartBeat = new Beat(0);
            [JsonProperty("bpm")]
            public float BeatPerMinute = 120;
        }
    }
    
}