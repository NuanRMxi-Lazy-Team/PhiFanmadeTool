#if !NETSTANDARD2_1
using System.Text.Json.Serialization;
using PhiFanmade.Core.Utils;

namespace PhiFanmade.Core.Common
{
    [JsonConverter(typeof(StjBeatJsonConverter))]
    public partial class Beat
    {
    }
}
#endif

