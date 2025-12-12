using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class ControlBase
        {
            [JsonProperty("easing")] public Easing Easing = new Easing(1);
            [JsonProperty("x")] public float X = 0.0f;
        }

        public class AlphaControl : ControlBase
        {
            [JsonProperty("alpha")] public float Alpha = 1.0f;

            public static readonly List<AlphaControl> Default = new List<AlphaControl>
            {
                new AlphaControl
                {
                    Easing = new Easing(1),
                    Alpha = 1.0f,
                    X = 0.0f
                },
                new AlphaControl
                {
                    Easing = new Easing(1),
                    Alpha = 1.0f,
                    X = 9999999.0f
                }
            };
        }

        public class XControl : ControlBase
        {
            [JsonProperty("pos")] public float Pos = 1.0f;

            public static readonly List<XControl> Default = new List<XControl>
            {
                new XControl
                {
                    Easing = new Easing(1),
                    Pos = 1.0f,
                    X = 0.0f
                },
                new XControl
                {
                    Easing = new Easing(1),
                    Pos = 1.0f,
                    X = 9999999.0f
                }
            };
        }

        public class SizeControl : ControlBase
        {
            [JsonProperty("size")] public float Size = 1.0f;

            public static readonly List<SizeControl> Default = new List<SizeControl>
            {
                new SizeControl
                {
                    Easing = new Easing(1),
                    Size = 1.0f,
                    X = 0.0f
                },
                new SizeControl
                {
                    Easing = new Easing(1),
                    Size = 1.0f,
                    X = 9999999.0f
                }
            };
        }

        public class SkewControl : ControlBase
        {
            [JsonProperty("skew")] public float Skew = 1.0f;

            public static readonly List<SkewControl> Default = new List<SkewControl>
            {
                new SkewControl
                {
                    Easing = new Easing(1),
                    Skew = 0.0f,
                    X = 0.0f
                },
                new SkewControl
                {
                    Easing = new Easing(1),
                    Skew = 0.0f,
                    X = 9999999.0f
                }
            };
        }

        public class YControl : ControlBase
        {
            [JsonProperty("y")] public float Y = 1.0f;

            public static readonly List<YControl> Default = new List<YControl>
            {
                new YControl
                {
                    Easing = new Easing(1),
                    Y = 1.0f,
                    X = 0.0f
                },
                new YControl
                {
                    Easing = new Easing(1),
                    Y = 1.0f,
                    X = 9999999.0f
                }
            };
        }
    }
}