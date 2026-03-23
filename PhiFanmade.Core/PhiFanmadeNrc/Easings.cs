using static PhiFanmade.Core.Utils.Easings;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public static class Easings
    {
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
                // Sine
                2 => EaseInSine,
                3 => EaseOutSine,
                4 => EaseInOutSine,
                // Quad
                5 => EaseInQuad,
                6 => EaseOutQuad,
                7 => EaseInOutQuad,
                // Cubic
                8 => EaseInCubic,
                9 => EaseOutCubic,
                10 => EaseInOutCubic,
                // Quart
                11 => EaseInQuart,
                12 => EaseOutQuart,
                13 => EaseInOutQuart,
                // Quint
                14 => EaseInQuint,
                15 => EaseOutQuint,
                16 => EaseInOutQuint,
                // Expo
                17 => EaseInExpo,
                18 => EaseOutExpo,
                19 => EaseInOutExpo,
                // Circ
                20 => EaseInCirc,
                21 => EaseOutCirc,
                22 => EaseInOutCirc,
                // Back
                23 => EaseInBack,
                24 => EaseOutBack,
                25 => EaseInOutBack,
                // Elastic
                26 => EaseInElastic,
                27 => EaseOutElastic,
                28 => EaseInOutElastic,
                // Bounce
                29 => EaseInBounce,
                30 => EaseOutBounce,
                31 => EaseInOutBounce,
                // Fallback
                _ => Linear
            };

            return Evaluate(function, start, end, t);
        }
    }

    public class Easing
    {
        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
        }

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