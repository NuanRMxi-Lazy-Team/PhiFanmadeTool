using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public abstract class ControlBase
        {
            [JsonProperty("easing")] public Easing Easing = new Easing(1);
            [JsonProperty("x")] public float X = 0.0f;

            public abstract ControlBase Clone();
        }

        public class AlphaControl : ControlBase
        {
            [JsonProperty("alpha")] public float Alpha = 1.0f;

            [JsonIgnore]
            public static List<AlphaControl> Default
            {
                get
                {
                    return new List<AlphaControl>
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
                    }.ConvertAll(input => input.Clone() as AlphaControl);
                }
            }

            public override ControlBase Clone()
            {
                // 深拷贝
                return new AlphaControl()
                {
                    Easing = new Easing(Easing),
                    X = X,
                    Alpha = Alpha
                };
            }
        }

        public class XControl : ControlBase
        {
            [JsonProperty("pos")] public float Pos = 1.0f;

            [JsonIgnore]
            public static List<XControl> Default
            {
                get
                {
                    return new List<XControl>
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
                    }.ConvertAll(input => input.Clone() as XControl);
                }
            }

            public override ControlBase Clone()
            {
                // 深拷贝
                return new XControl()
                {
                    Easing = new Easing(Easing),
                    X = X,
                    Pos = Pos
                };
            }
        }

        public class SizeControl : ControlBase
        {
            [JsonProperty("size")] public float Size = 1.0f;

            [JsonIgnore]
            public static List<SizeControl> Default
            {
                get
                {
                    return new List<SizeControl>
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
                    }.ConvertAll(input => input.Clone() as SizeControl);
                }
            }

            public override ControlBase Clone()
            {
                // 深拷贝
                return new SizeControl()
                {
                    Easing = new Easing(Easing),
                    X = X,
                    Size = Size
                };
            }
        }

        public class SkewControl : ControlBase
        {
            [JsonProperty("skew")] public float Skew = 1.0f;

            [JsonIgnore]
            public static List<SkewControl> Default
            {
                get
                {
                    return new List<SkewControl>
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
                    }.ConvertAll(input => input.Clone() as SkewControl);
                }
            }

            public override ControlBase Clone()
            {
                // 深拷贝
                return new SkewControl()
                {
                    Easing = new Easing(Easing),
                    X = X,
                    Skew = Skew
                };
            }
        }

        public class YControl : ControlBase
        {
            [JsonProperty("y")] public float Y = 1.0f;

            [JsonIgnore]
            public static List<YControl> Default
            {
                get
                {
                    return new List<YControl>
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
                    }.ConvertAll(input => input.Clone() as YControl);
                }
            }

            public override ControlBase Clone()
            {
                // 深拷贝
                return new YControl()
                {
                    Easing = new Easing(Easing),
                    X = X,
                    Y = Y
                };
            }
        }
    }
}