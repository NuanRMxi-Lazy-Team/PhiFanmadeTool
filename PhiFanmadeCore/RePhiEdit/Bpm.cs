using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class Bpm
        {
            [JsonProperty("bpm")] public float BeatPerMinute = 120f;
            [JsonProperty("startTime")] public Beat StartTime = new Beat(new[] { 0, 0, 1 });
        }
    }
}

