using System;
using System.Collections.Generic;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public abstract class ControlBase
    {
        protected readonly Guid Id = Guid.NewGuid();
        public Easing Easing { get; set; } = new(1);

        public float X { get; set; }

        public abstract ControlBase Clone();
    }

    public sealed class AlphaControl : ControlBase, IEquatable<AlphaControl>
    {
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(AlphaControl other)
        {
            if (other == null) return false;
            if (other.GetHashCode() == GetHashCode()) return true;

            // 比较所有需要比较的数值属性
            return Math.Abs(Alpha - other.Alpha) < 1e-6
                   && Math.Abs(X - other.X) < 1e-6
                   && (Easing?.Equals(other.Easing) ?? other.Easing == null);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AlphaControl);
        }

        public float Alpha { get; set; } = 1.0f;

        public static List<AlphaControl> Default
        {
            get
            {
                return new List<AlphaControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Alpha = 1.0f,
                        X = 0.0f
                    },
                    new()
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

    public sealed class XControl : ControlBase, IEquatable<XControl>
    {
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(XControl other)
        {
            if (other == null) return false;
            if (other.GetHashCode() == GetHashCode()) return true;
            
            return Math.Abs(Pos - other.Pos) < 1e-6
                   && Math.Abs(X - other.X) < 1e-6
                   && (Easing?.Equals(other.Easing) ?? other.Easing == null);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as XControl);
        }

        public float Pos { get; set; } = 1.0f;

        public static List<XControl> Default
        {
            get
            {
                return new List<XControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Pos = 1.0f,
                        X = 0.0f
                    },
                    new()
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

    public sealed class SizeControl : ControlBase, IEquatable<SizeControl>
    {
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(SizeControl other)
        {
            if (other == null) return false;
            if (other.GetHashCode() == GetHashCode()) return true;

            return Math.Abs(Size - other.Size) < 1e-6
                   && Math.Abs(X - other.X) < 1e-6
                   && (Easing?.Equals(other.Easing) ?? other.Easing == null);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SizeControl);
        }

        public float Size { get; set; } = 1.0f;

        public static List<SizeControl> Default
        {
            get
            {
                return new List<SizeControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Size = 1.0f,
                        X = 0.0f
                    },
                    new()
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

    public sealed class SkewControl : ControlBase, IEquatable<SkewControl>
    {
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(SkewControl other)
        {
            if (other == null) return false;
            if (other.GetHashCode() == GetHashCode()) return true;
            
            return Math.Abs(Skew - other.Skew) < 1e-6
                   && Math.Abs(X - other.X) < 1e-6
                   && (Easing?.Equals(other.Easing) ?? other.Easing == null);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SkewControl);
        }

        public float Skew { get; set; } = 1.0f;

        public static List<SkewControl> Default
        {
            get
            {
                return new List<SkewControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Skew = 0.0f,
                        X = 0.0f
                    },
                    new()
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

    public sealed class YControl : ControlBase, IEquatable<YControl>
    {
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(YControl other)
        {
            if (other == null) return false;
            if (other.GetHashCode() == GetHashCode()) return true;

            return Math.Abs(Y - other.Y) < 1e-6
                   && Math.Abs(X - other.X) < 1e-6
                   && (Easing?.Equals(other.Easing) ?? other.Easing == null);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as YControl);
        }


        public float Y { get; set; } = 1.0f;

        public static List<YControl> Default
        {
            get
            {
                return new List<YControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Y = 1.0f,
                        X = 0.0f
                    },
                    new()
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