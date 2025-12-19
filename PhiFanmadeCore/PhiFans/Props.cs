using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public static partial class PhiFans
    {
        public class Props
        {
            [JsonProperty("speed")] public List<Event> Speed = new List<Event>();
            [JsonProperty("positionX")] public List<Event> PositionX = new List<Event>();
            [JsonProperty("positionY")] public List<Event> PositionY = new List<Event>();
            [JsonProperty("rotate")] public List<Event> Rotate = new List<Event>();
            [JsonProperty("alpha")] public List<Event> Alpha = new List<Event>();
        }
    }
}