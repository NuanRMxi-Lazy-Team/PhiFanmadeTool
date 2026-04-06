using Newtonsoft.Json;
using PhiFanmade.Core.RePhiEdit.JsonConverter;
using static PhiFanmade.Core.Utils.Easings;

namespace PhiFanmade.Core.RePhiEdit
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
    public class Easing
    {
        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
        }

        [JsonIgnore]
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