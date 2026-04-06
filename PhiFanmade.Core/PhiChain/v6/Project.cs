using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiChain.v6
{
    public sealed class ProjectMeta
    {
        [JsonProperty("composer")]
        public string Composer { get; set; } = string.Empty;

        [JsonProperty("charter")]
        public string Charter { get; set; } = string.Empty;

        [JsonProperty("illustrator")]
        public string Illustrator { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("level")]
        public string Level { get; set; } = string.Empty;
    }
}

