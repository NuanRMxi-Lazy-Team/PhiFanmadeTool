using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhiFanmade.Core.PhiChain.v6.JsonConverter;

namespace PhiFanmade.Core.PhiChain.v6
{
    public enum EasingKind
    {
        Linear,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
        Custom,
        Steps,
        Elastic
    }

    [JsonConverter(typeof(EasingJsonConverter))]
    public sealed class Easing
    {
        public static Easing Linear => new Easing { Kind = EasingKind.Linear };

        [JsonIgnore] public EasingKind Kind { get; set; } = EasingKind.Linear;

        [JsonIgnore] public float X1 { get; set; }

        [JsonIgnore] public float Y1 { get; set; }

        [JsonIgnore] public float X2 { get; set; }

        [JsonIgnore] public float Y2 { get; set; }

        [JsonIgnore] public int Count { get; set; }

        [JsonIgnore] public float Omega { get; set; }
    }
}