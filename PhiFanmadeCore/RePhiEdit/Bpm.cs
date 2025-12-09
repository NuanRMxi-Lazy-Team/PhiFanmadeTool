using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public class Bpm
    {
        [JsonProperty("bpm")] public float BeatPerMinute = 120f;
        [JsonProperty("startTime")] public Beat StartTime = new Beat(new[]{0,0,1});

        public override string ToString()
        {
            return $"bp {BeatPerMinute} {StartTime}";
        }
    }
}

