using System;
using Newtonsoft.Json;

namespace KaedePhi.Core.RePhiEdit.JsonConverter
{
    public class AttachUiConverter : JsonConverter<AttachUi?>
    {
        public override void WriteJson(JsonWriter writer, AttachUi? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            switch (value)
            {
                case AttachUi.Bar
                    :
                    writer.WriteValue("bar");
                    break;
                case AttachUi.Combo
                    :
                    writer.WriteValue("combo");
                    break;
                case AttachUi.ComboNumber
                    : // ReSharper disable once StringLiteralTypo
                    writer.WriteValue("combonumber");
                    break;
                case AttachUi.Level
                    :
                    writer.WriteValue("level");
                    break;
                case AttachUi.Name
                    :
                    writer.WriteValue("name");
                    break;
                case AttachUi.Pause
                    :
                    writer.WriteValue("pause");
                    break;
                case AttachUi.Score
                    :
                    writer.WriteValue("score");
                    break;
                default:
                    writer.WriteNull();
                    break;
            }
        }

        public override AttachUi? ReadJson(JsonReader reader, Type objectType, AttachUi? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (reader.TokenType == JsonToken.String)
            {
                var value = (string)reader.Value;
                var lowerValue = value!.ToLower();
                return lowerValue switch
                {
                    "bar" => AttachUi.Bar,
                    "combo" => AttachUi.Combo,
                    // ReSharper disable once StringLiteralTypo
                    "combonumber" => AttachUi.ComboNumber,
                    "level" => AttachUi.Level,
                    "name" => AttachUi.Name,
                    "pause" => AttachUi.Pause,
                    "score" => AttachUi.Score,
                    _ => existingValue
                };
            }

            return existingValue;
        }
    }
}