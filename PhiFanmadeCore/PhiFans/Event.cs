using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Event
        {
            [JsonProperty("beat")] public Beat Beat = new Beat(0);
            [JsonProperty("value")] public float Value;
            [JsonProperty("continuous")] public bool Continuous;
            [JsonProperty("easing")] public int Easing;
        }
    }
}