using Newtonsoft.Json;
using System;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public static class Easings
        {
            // Delegate for easing functions
            public delegate double EasingFunction(double t);

            // Linear
            private static double Linear(double t) => t;

            // Quadratic
            private static double EaseInQuad(double t) => t * t;
            private static double EaseOutQuad(double t) => t * (2 - t);

            private static double EaseInOutQuad(double t) =>
                t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;

            // Cubic
            private static double EaseInCubic(double t) => t * t * t;

            private static double EaseOutCubic(double t)
            {
                t--;
                return t * t * t + 1;
            }

            private static double EaseInOutCubic(double t) =>
                t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;

            // Quartic
            private static double EaseInQuart(double t) => t * t * t * t;

            private static double EaseOutQuart(double t)
            {
                t--;
                return 1 - t * t * t * t;
            }

            private static double EaseInOutQuart(double t) =>
                t < 0.5 ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;

            // Quintic
            private static double EaseInQuint(double t) => t * t * t * t * t;

            private static double EaseOutQuint(double t)
            {
                t--;
                return t * t * t * t * t + 1;
            }

            private static double EaseInOutQuint(double t) =>
                t < 0.5 ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;

            // Sine
            private static double EaseInSine(double t) =>
                1 - Math.Cos(t * Math.PI / 2);

            private static double EaseOutSine(double t) =>
                Math.Sin(t * Math.PI / 2);

            private static double EaseInOutSine(double t) =>
                -0.5f * (Math.Cos(Math.PI * t) - 1);

            // Exponential
            private static double EaseInExpo(double t) =>
                t == 0 ? 0 : Math.Pow(2, 10 * (t - 1));

            private static double EaseOutExpo(double t) =>
                t == 1 ? 1 : 1 - Math.Pow(2, -10 * t);

            private static double EaseInOutExpo(double t)
            {
                if (t == 0 || t == 1) return t;
                return t < 0.5f
                    ? 0.5f * Math.Pow(2, 20 * t - 10)
                    : 1 - 0.5f * Math.Pow(2, -20 * t + 10);
            }

            // Circular
            private static double EaseInCirc(double t) =>
                1 - Math.Sqrt(1 - t * t);

            private static double EaseOutCirc(double t) =>
                Math.Sqrt(1 - (--t) * t);

            private static double EaseInOutCirc(double t) =>
                t < 0.5f
                    ? 0.5f * (1 - Math.Sqrt(1 - 4 * t * t))
                    : 0.5f * (Math.Sqrt(1 - 4 * (--t) * t) + 1);

            // Back
            private static double EaseInBack(double t)
            {
                const double s = 1.70158f;
                return t * t * ((s + 1) * t - s);
            }

            private static double EaseOutBack(double t)
            {
                const double s = 1.70158f;
                t--;
                return t * t * ((s + 1) * t + s) + 1;
            }

            private static double EaseInOutBack(double t)
            {
                const double s = 1.70158f * 1.525f;
                t *= 2;
                if (t < 1)
                    return 0.5f * (t * t * ((s + 1) * t - s));
                t -= 2;
                return 0.5f * (t * t * ((s + 1) * t + s) + 2);
            }

            // Elastic
            private static double EaseInElastic(double t)
            {
                if (t == 0 || t == 1) return t;
                return -Math.Pow(2, 10 * (t - 1)) *
                       Math.Sin((t - 1.1f) * 5 * Math.PI);
            }

            private static double EaseOutElastic(double t)
            {
                if (t == 0 || t == 1) return t;
                return Math.Pow(2, -10 * t) *
                    Math.Sin((t - 0.1f) * 5 * Math.PI) + 1;
            }

            private static double EaseInOutElastic(double t)
            {
                if (t == 0 || t == 1) return t;
                t *= 2;
                if (t < 1)
                    return -0.5f * Math.Pow(2, 10 * (t - 1)) *
                           Math.Sin((t - 1.1f) * 5 * Math.PI);
                t--;
                return Math.Pow(2, -10 * t) *
                    Math.Sin((t - 0.1f) * 5 * Math.PI) * 0.5f + 1;
            }

            // Bounce
            private static double EaseInBounce(double t) =>
                1 - EaseOutBounce(1 - t);

            private static double EaseOutBounce(double t)
            {
                const double n1 = 7.5625f;
                const double d1 = 2.75f;
                if (t < 1 / d1)
                    return n1 * t * t;
                else if (t < 2 / d1)
                {
                    t -= 1.5f / d1;
                    return n1 * t * t + 0.75f;
                }
                else if (t < 2.5 / d1)
                {
                    t -= 2.25f / d1;
                    return n1 * t * t + 0.9375f;
                }
                else
                {
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
            }

            private static double EaseInOutBounce(double t) =>
                t < 0.5f
                    ? (1 - EaseOutBounce(1 - 2 * t)) * 0.5f
                    : (EaseOutBounce(2 * t - 1) + 1) * 0.5f;

            // Method to evaluate easing between any start and end point
            private static double Evaluate(EasingFunction function, double start, double end, double t)
            {
                // code by PhiZone Player
                double progress = function(start + (end - start) * t);
                double progressStart = function(start);
                double progressEnd = function(end);
                return (progress - progressStart) / (progressEnd - progressStart);
            }

            // Overload, using int to specify the corresponding EasingFunction
            public static double Evaluate(int easingType, double start, double end, double t)
            {
                EasingFunction function = easingType switch
                {
                    1 => Linear,
                    2 => EaseOutSine,
                    3 => EaseInSine,
                    4 => EaseOutQuad,
                    5 => EaseInQuad,
                    6 => EaseInOutSine,
                    7 => EaseInOutQuad,
                    8 => EaseOutCubic,
                    9 => EaseInCubic,
                    10 => EaseOutQuart,
                    11 => EaseInQuart,
                    12 => EaseInOutCubic,
                    13 => EaseInOutQuart,
                    14 => EaseOutQuint,
                    15 => EaseInQuint,
                    16 => EaseOutExpo,
                    17 => EaseInExpo,
                    18 => EaseOutCirc,
                    19 => EaseInCirc,
                    20 => EaseOutBack,
                    21 => EaseInBack,
                    22 => EaseInOutCirc,
                    23 => EaseInOutBack,
                    24 => EaseOutElastic,
                    25 => EaseInElastic,
                    26 => EaseOutBounce,
                    27 => EaseInBounce,
                    28 => EaseInOutBounce,
                    29 => EaseInOutElastic,
                    _ => Linear
                };

                return Evaluate(function, start, end, t);
            }
        }

        [JsonConverter(typeof(EasingJsonConverter))]
#if !NETSTANDARD2_1
        [System.Text.Json.Serialization.JsonConverter(typeof(StjEasingJsonConverter))]
#endif
        public class Easing
        {
            public Easing(int easingNumber)
            {
                _easingNumber = easingNumber;
            }

            [JsonIgnore]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonIgnore]
#endif
            private int _easingNumber;

            public float Do(float minLim, float maxLim, float start, float end, float t)
            {
                var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
                //插值后返回
                return (float)(start + (end - start) * easedTime);
            }

            public double Do(float minLim, float maxLim, double start, double end, double t)
            {
                var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
                //插值后返回
                return start + (end - start) * easedTime;
            }

            public int Do(float minLim, float maxLim, int start, int end, float t)
            {
                var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
                //插值后返回
                return (int)(start + (end - start) * easedTime);
            }

            public byte Do(float minLim, float maxLim, byte start, byte end, float t)
            {
                var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
                //插值后返回
                return (byte)(start + (end - start) * easedTime);
            }

            // 以int访问时，返回缓动编号
            public static implicit operator int(Easing easing) => easing._easingNumber;
        }
    }
}