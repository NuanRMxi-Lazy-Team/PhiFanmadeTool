using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhiFanmade.Core.PhiChain.v6.JsonConverter
{
    public sealed class EasingJsonConverter : JsonConverter<Easing>
    {
        public override void WriteJson(JsonWriter writer, Easing value, JsonSerializer serializer)
        {
            var obj = new JObject
            {
                ["type"] = ToTypeString(value.Kind)
            };

            if (value.Kind == EasingKind.Custom)
            {
                obj["x1"] = value.X1;
                obj["y1"] = value.Y1;
                obj["x2"] = value.X2;
                obj["y2"] = value.Y2;
            }
            else if (value.Kind == EasingKind.Steps)
            {
                obj["count"] = value.Count;
            }
            else if (value.Kind == EasingKind.Elastic)
            {
                obj["omega"] = value.Omega;
            }

            obj.WriteTo(writer);
        }

        public override Easing ReadJson(JsonReader reader, Type objectType, Easing existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var easing = existingValue ?? new Easing();
            easing.Kind = ParseType(obj.Value<string>("type"));

            if (easing.Kind == EasingKind.Custom)
            {
                easing.X1 = obj.Value<float?>("x1") ?? 0f;
                easing.Y1 = obj.Value<float?>("y1") ?? 0f;
                easing.X2 = obj.Value<float?>("x2") ?? 0f;
                easing.Y2 = obj.Value<float?>("y2") ?? 0f;
            }
            else if (easing.Kind == EasingKind.Steps)
            {
                easing.Count = obj.Value<int?>("count") ?? 0;
            }
            else if (easing.Kind == EasingKind.Elastic)
            {
                easing.Omega = obj.Value<float?>("omega") ?? 0f;
            }

            return easing;
        }

        private static string ToTypeString(EasingKind kind)
        {
            switch (kind)
            {
                case EasingKind.Linear: return "linear";
                case EasingKind.EaseInSine: return "ease_in_sine";
                case EasingKind.EaseOutSine: return "ease_out_sine";
                case EasingKind.EaseInOutSine: return "ease_in_out_sine";
                case EasingKind.EaseInQuad: return "ease_in_quad";
                case EasingKind.EaseOutQuad: return "ease_out_quad";
                case EasingKind.EaseInOutQuad: return "ease_in_out_quad";
                case EasingKind.EaseInCubic: return "ease_in_cubic";
                case EasingKind.EaseOutCubic: return "ease_out_cubic";
                case EasingKind.EaseInOutCubic: return "ease_in_out_cubic";
                case EasingKind.EaseInQuart: return "ease_in_quart";
                case EasingKind.EaseOutQuart: return "ease_out_quart";
                case EasingKind.EaseInOutQuart: return "ease_in_out_quart";
                case EasingKind.EaseInQuint: return "ease_in_quint";
                case EasingKind.EaseOutQuint: return "ease_out_quint";
                case EasingKind.EaseInOutQuint: return "ease_in_out_quint";
                case EasingKind.EaseInExpo: return "ease_in_expo";
                case EasingKind.EaseOutExpo: return "ease_out_expo";
                case EasingKind.EaseInOutExpo: return "ease_in_out_expo";
                case EasingKind.EaseInCirc: return "ease_in_circ";
                case EasingKind.EaseOutCirc: return "ease_out_circ";
                case EasingKind.EaseInOutCirc: return "ease_in_out_circ";
                case EasingKind.EaseInBack: return "ease_in_back";
                case EasingKind.EaseOutBack: return "ease_out_back";
                case EasingKind.EaseInOutBack: return "ease_in_out_back";
                case EasingKind.EaseInElastic: return "ease_in_elastic";
                case EasingKind.EaseOutElastic: return "ease_out_elastic";
                case EasingKind.EaseInOutElastic: return "ease_in_out_elastic";
                case EasingKind.EaseInBounce: return "ease_in_bounce";
                case EasingKind.EaseOutBounce: return "ease_out_bounce";
                case EasingKind.EaseInOutBounce: return "ease_in_out_bounce";
                case EasingKind.Custom: return "custom";
                case EasingKind.Steps: return "steps";
                case EasingKind.Elastic: return "elastic";
                default:
                    throw new JsonSerializationException("Unknown easing kind.");
            }
        }

        private static EasingKind ParseType(string type)
        {
            switch (type)
            {
                case "linear": return EasingKind.Linear;
                case "ease_in_sine": return EasingKind.EaseInSine;
                case "ease_out_sine": return EasingKind.EaseOutSine;
                case "ease_in_out_sine": return EasingKind.EaseInOutSine;
                case "ease_in_quad": return EasingKind.EaseInQuad;
                case "ease_out_quad": return EasingKind.EaseOutQuad;
                case "ease_in_out_quad": return EasingKind.EaseInOutQuad;
                case "ease_in_cubic": return EasingKind.EaseInCubic;
                case "ease_out_cubic": return EasingKind.EaseOutCubic;
                case "ease_in_out_cubic": return EasingKind.EaseInOutCubic;
                case "ease_in_quart": return EasingKind.EaseInQuart;
                case "ease_out_quart": return EasingKind.EaseOutQuart;
                case "ease_in_out_quart": return EasingKind.EaseInOutQuart;
                case "ease_in_quint": return EasingKind.EaseInQuint;
                case "ease_out_quint": return EasingKind.EaseOutQuint;
                case "ease_in_out_quint": return EasingKind.EaseInOutQuint;
                case "ease_in_expo": return EasingKind.EaseInExpo;
                case "ease_out_expo": return EasingKind.EaseOutExpo;
                case "ease_in_out_expo": return EasingKind.EaseInOutExpo;
                case "ease_in_circ": return EasingKind.EaseInCirc;
                case "ease_out_circ": return EasingKind.EaseOutCirc;
                case "ease_in_out_circ": return EasingKind.EaseInOutCirc;
                case "ease_in_back": return EasingKind.EaseInBack;
                case "ease_out_back": return EasingKind.EaseOutBack;
                case "ease_in_out_back": return EasingKind.EaseInOutBack;
                case "ease_in_elastic": return EasingKind.EaseInElastic;
                case "ease_out_elastic": return EasingKind.EaseOutElastic;
                case "ease_in_out_elastic": return EasingKind.EaseInOutElastic;
                case "ease_in_bounce": return EasingKind.EaseInBounce;
                case "ease_out_bounce": return EasingKind.EaseOutBounce;
                case "ease_in_out_bounce": return EasingKind.EaseInOutBounce;
                case "custom": return EasingKind.Custom;
                case "steps": return EasingKind.Steps;
                case "elastic": return EasingKind.Elastic;
                default:
                    throw new JsonSerializationException("Unsupported easing type: " + type);
            }
        }
    }
}