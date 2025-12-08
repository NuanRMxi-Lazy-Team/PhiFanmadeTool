using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class ControlBase
{
    [JsonProperty("easing")] public Easing Easing = new(1);
    [JsonProperty("x")] public float X = 0.0f;
}

public class AlphaControl : ControlBase
{
    [JsonProperty("alpha")] public float Alpha = 1.0f;
}

public class PosControl : ControlBase
{
    [JsonProperty("pos")] public float Pos = 1.0f;
}

public class SizeControl : ControlBase
{
    [JsonProperty("size")] public float Size = 1.0f;
}

public class SkewControl : ControlBase
{
    [JsonProperty("skew")] public float Skew = 1.0f;
}

public class YControl : ControlBase
{
    [JsonProperty("y")] public float Y = 1.0f;
}