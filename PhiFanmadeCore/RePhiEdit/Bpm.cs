using Newtonsoft.Json;
namespace PhiFanmade.Core.RePhiEdit;

public class Bpm
{
    [JsonProperty("bpm")] public float BeatPerMinute = 120f;
    [JsonProperty("startTime")] public Beat StartTime = new Beat([0,0,1]);
}