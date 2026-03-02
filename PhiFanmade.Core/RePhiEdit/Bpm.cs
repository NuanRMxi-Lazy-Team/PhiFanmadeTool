using System;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.RePhiEdit
{
    public class BpmItem
    {
        [JsonProperty("bpm")]
        public float BeatPerMinute = 120f;
        [JsonIgnore]
        public float Bpm => BeatPerMinute;

        [JsonProperty("startTime")]
        public Beat StartTime = new Beat(new[] { 0, 0, 1 });

        public BpmItem Clone()
        {
            return new BpmItem()
            {
                BeatPerMinute = BeatPerMinute,
                StartTime = new Beat((int[])StartTime)
            };
        }
    }

    [Obsolete("由于Bpm类名容易产生争议，请改用BpmItem", false)]
    public class Bpm : BpmItem
    { }
}