using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiChain.v6
{
    public sealed class Line
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "Unnamed Line";
    }
}

