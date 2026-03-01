using Newtonsoft.Json;
using PhiFanmade.Core.Common;
namespace PhiFanmade.Core.RePhiEdit
{
    public class Bpm
    {
            [JsonProperty("bpm")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("bpm")]
#endif
            public float BeatPerMinute = 120f;

            [JsonProperty("startTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("startTime")]
#endif
            public Beat StartTime = new Beat(new[] { 0, 0, 1 });
            public Bpm Clone()
            {
                return new Bpm()
                {
                    BeatPerMinute = BeatPerMinute,
                    StartTime = new Beat((int[])StartTime)
                };
            }
        }
}

