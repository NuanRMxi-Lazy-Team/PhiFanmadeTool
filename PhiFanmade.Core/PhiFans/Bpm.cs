using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFans
{
    public class Bpm
    {
        [JsonProperty("beat")] public Beat StartBeat = new Beat(0);
        [JsonProperty("bpm")] public float BeatPerMinute = 120;
    }
}