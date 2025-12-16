using System.Linq;
using Newtonsoft.Json;
// STJ 特性均使用完全限定名，避免命名冲突

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class Note
        {
            /// <summary>
            /// 音符是否在判定线上方下落，true为上方，false为下方
            /// </summary>
            [JsonProperty("above")] [Newtonsoft.Json.JsonConverter(typeof(BoolConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("above"), System.Text.Json.Serialization.JsonConverter(typeof(StjBoolConverter))]
#endif
            public bool Above = true;

            /// <summary>
            /// 音符的不透明度
            /// </summary>
            [JsonProperty("alpha")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("alpha")]
#endif
            public int Alpha = 255;

            /// <summary>
            /// 音符的起始拍
            /// </summary>
            [JsonProperty("startTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("startTime")]
#endif
            public Beat StartBeat = new Beat(new[] { 0, 0, 1 }); // 开始时间

            /// <summary>
            /// 音符的结束拍
            /// </summary>
            [JsonProperty("endTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("endTime")]
#endif
            public Beat EndBeat = new Beat(new[] { 1, 0, 1 }); // 结束时间

            /// <summary>
            /// 音符是否为假音符
            /// </summary>
            [JsonProperty("isFake")] [Newtonsoft.Json.JsonConverter(typeof(BoolConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("isFake"), System.Text.Json.Serialization.JsonConverter(typeof(StjBoolConverter))]
#endif
            public bool IsFake = false;

            /// <summary>
            /// 音符相对于判定线的X坐标
            /// </summary>
            [JsonProperty("positionX")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("positionX")]
#endif
            public float PositionX = 0.0f; // X坐标

            /// <summary>
            /// 音符宽度倍率
            /// </summary>
            [JsonProperty("size")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("size")]
#endif
            public float Size = 1.0f; // 宽度倍率

            /// <summary>
            /// 音符判定宽度倍率
            /// </summary>
            [JsonProperty("judgeArea")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("judgeArea")]
#endif
            public float JudgeArea = 1.0f;

            /// <summary>
            /// 音符下落速度倍率
            /// </summary>
            [JsonProperty("speed")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("speed")]
#endif
            public float SpeedMultiplier = 1.0f; // 速度倍率

            /// <summary>
            /// 音符类型
            /// </summary>
            [JsonProperty("type")] [Newtonsoft.Json.JsonConverter(typeof(NoteTypeConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("type"), System.Text.Json.Serialization.JsonConverter(typeof(StjNoteTypeConverter))]
#endif
            public NoteType Type = NoteType.Tap; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）

            /// <summary>
            /// 音符可见时间，单位为秒
            /// </summary>
            [JsonProperty("visibleTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("visibleTime")]
#endif
            public float VisibleTime = 999999.0000f; // 可见时间（单位为秒）

            /// <summary>
            /// 音符相对于判定线的Y轴偏移
            /// </summary>
            [JsonProperty("yOffset")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("yOffset")]
#endif
            public float YOffset = 0.0f; // Y偏移

            /// <summary>
            /// 音符颜色（RGB，顶点颜色乘法），此字段在Json中优先为tint，早期版本使用过color字段
            /// </summary>
            [Newtonsoft.Json.JsonConverter(typeof(ColorConverter))] [JsonProperty("tint")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("tint"), System.Text.Json.Serialization.JsonConverter(typeof(StjColorConverter))]
#endif
            public byte[] Color = { 255, 255, 255 }; // 颜色（RGB）

            [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(ColorConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("color"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull), System.Text.Json.Serialization.JsonConverter(typeof(StjColorConverter))]
#endif
            private byte[] ColorLegacyField
            {
                get => null; // 序列化时不输出
                set
                {
                    if (value != null) Color = value;
                } // 反序列化时赋值
            }

            /// <summary>
            /// 打击特效颜色（RGB，顶点颜色乘法）
            /// </summary>
            [JsonProperty("tintHitEffects", NullValueHandling = NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(ColorConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("tintHitEffects"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull), System.Text.Json.Serialization.JsonConverter(typeof(StjColorConverter))]
#endif
            public byte[] HitFxColor = null;

            /// <summary>
            /// 音符打击音效相对路径
            /// </summary>
            [JsonProperty("hitsound", NullValueHandling = NullValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("hitsound"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
#endif
            public string HitSound = null; // 音效
            public Note Clone()
            {
                //return (Note)this.MemberwiseClone();
                // 有Beat，不能使用MemberwiseClone
                return new Note
                {
                    Above = Above,
                    Alpha = Alpha,
                    StartBeat = new Beat((int[])StartBeat),
                    EndBeat = new Beat((int[])EndBeat),
                    IsFake = IsFake,
                    PositionX = PositionX,
                    Size = Size,
                    JudgeArea = JudgeArea,
                    SpeedMultiplier = SpeedMultiplier,
                    Type = Type,
                    VisibleTime = VisibleTime,
                    YOffset = YOffset,
                    Color = Color.ToArray(),
                    HitFxColor = HitFxColor != null ? HitFxColor.ToArray() : null,
                    HitSound = HitSound
                };
            }
        }

        public enum NoteType
        {
            Tap = 1,
            Hold = 2,
            Flick = 3,
            Drag = 4
        }
    }
}