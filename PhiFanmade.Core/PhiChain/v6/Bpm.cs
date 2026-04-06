using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;
using PhiFanmade.Core.PhiChain.v6.JsonConverter;

namespace PhiFanmade.Core.PhiChain.v6
{
    public sealed class BpmPoint
    {
        [JsonProperty("beat")] public Beat Beat { get; set; } = new Beat(new[] { 0, 0, 1 });

        [JsonProperty("bpm")] public float Bpm { get; set; } = 120f;

        [JsonIgnore] public float Time { get; internal set; }
    }

    [JsonConverter(typeof(BpmListJsonConverter))]
    public sealed class BpmList : List<BpmPoint>
    {
        public BpmList()
        {
            Add(new BpmPoint());
        }

        public BpmList(IEnumerable<BpmPoint> points)
            : base(points)
        {
            if (Count == 0)
            {
                Add(new BpmPoint());
            }

            ComputeTimes();
        }

        public void ComputeTimes()
        {
            var time = 0f;
            var lastBeat = 0f;
            var lastBpm = -1f;
            for (var i = 0; i < Count; i++)
            {
                var point = this[i];
                if (Math.Abs(lastBpm - (-1f)) > float.Epsilon)
                {
                    time += ((float)point.Beat - lastBeat) * (60f / lastBpm);
                }

                lastBeat = point.Beat;
                lastBpm = point.Bpm;
                point.Time = time;
            }
        }
    }
}