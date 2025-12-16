using System;
using System.Collections.Generic;
using Newtonsoft.Json;
// STJ 特性使用完全限定名

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class JudgeLine
        {
            /// <summary>
            /// 判定线名称
            /// </summary>
            [JsonProperty("Name")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("Name")]
#endif
            public string Name = "PhiFanmadeCoreJudgeLine";

            /// <summary>
            /// 判定线纹理相对路径，默认值为line.png
            /// </summary>
            [JsonProperty("Texture")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("Texture")]
#endif
            public string Texture = "line.png"; // 判定线纹理路径

            /// <summary>
            /// 判定线纹理锚点(0~1之间)，默认值为中心点(0.5, 0.5)
            /// </summary>
            [JsonProperty("anchor")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("anchor")]
#endif
            public float[] Anchor = { 0.5f, 0.5f }; // 判定线纹理锚点

            /// <summary>
            /// 判定线事件层列表
            /// </summary>
            [JsonProperty("eventLayers", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("eventLayers")]
#endif
            public List<EventLayer> EventLayers = new List<EventLayer>(); // 事件层

            /// <summary>
            /// 父级判定线索引，-1表示无父级
            /// </summary>
            [JsonProperty("father")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("father")]
#endif
            public int Father = -1; // 父级

            /// <summary>
            /// 是否遮罩越过判定线的音符（已被打击的除外）
            /// </summary>
            [JsonProperty("isCover")] [Newtonsoft.Json.JsonConverter(typeof(BoolConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("isCover"), System.Text.Json.Serialization.JsonConverter(typeof(StjBoolConverter))]
#endif
            public bool IsCover = true; // 是否遮罩

            /// <summary>
            /// 判定线音符列表
            /// </summary>
            [JsonProperty("notes")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("notes")]
#endif
            public List<Note> Notes = new List<Note>(); // note列表

            /// <summary>
            /// Note总数量(包含 FakeNote，不包含任何形式的Hold)。
            /// 为什么？RePhiEdit就是这样设计的。。。
            /// </summary>
            [JsonProperty("numOfNotes")]
            [Obsolete("你不能修改这个值，也不应该读取这个值，这个值完全不准确", true)]
            public int TotalNumberOfNotes
            {
                get
                {
                    // Note总数量(包含 FakeNote，不包含任何形式的Hold)
                    int count = 0;
                    foreach (var note in Notes)
                    {
                        if (note.Type == NoteType.Hold)
                            continue;
                        count++;
                    }

                    return count;
                }
            }

            /// <summary>
            /// 特殊事件层（故事板）
            /// </summary>
            [JsonProperty("extended", DefaultValueHandling = DefaultValueHandling.Ignore)]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("extended")]
#endif
            public ExtendLayer Extended = new ExtendLayer();

            /// <summary>
            /// 判定线的Z轴顺序
            /// </summary>
            [JsonProperty("zOrder")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("zOrder")]
#endif
            public int ZOrder; // Z轴顺序

            /// <summary>
            /// 判定线是否绑定UI
            /// </summary>
            [JsonProperty("attachUI", NullValueHandling = NullValueHandling.Ignore)]
            [Newtonsoft.Json.JsonConverter(typeof(AttachUiConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("attachUI"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull), System.Text.Json.Serialization.JsonConverter(typeof(StjAttachUiConverter))]
#endif
            public AttachUi? AttachUi = null; // 绑定UI名，当不绑定时为null

            /// <summary>
            /// 判定线纹理是否为GIF
            /// </summary>
            [JsonProperty("isGif")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("isGif")]
#endif
            public bool IsGif; // 纹理是否为GIF

            /// <summary>
            /// 所属组
            /// </summary>
            [JsonProperty("Group")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("Group")]
#endif
            public int Group = 0; // 绑定组

            /// <summary>
            /// 当前判定线相对于当前BPM的因子。判定线BPM = 谱面BPM / BpmFactor
            /// </summary>
            [JsonProperty("bpmfactor")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("bpmfactor")]
#endif
            public float BpmFactor = 1.0f; // BPM因子

            /// <summary>
            /// 是否跟随父线旋转
            /// </summary>
            [JsonProperty("rotateWithFather")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("rotateWithFather")]
#endif
            public bool RotateWithFather = false; // 是否随父级旋转

            /// <summary>
            /// Position（X） Control 控制点列表
            /// </summary>
            [JsonProperty("posControl")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("posControl")]
#endif
            public List<XControl> PositionControls
            {
                get
                {
                    if (_positionControls == null)
                    {
                        _positionControls = new List<XControl>();
                    }
                    return _positionControls;
                }
                set => _positionControls = value;
            }

            private List<XControl> _positionControls;

            /// <summary>
            /// Alpha Control 控制点列表
            /// </summary>
            [JsonProperty("alphaControl")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("alphaControl")]
#endif
            public List<AlphaControl> AlphaControls
            {
                get
                {
                    if (_alphaControls == null)
                    {
                        _alphaControls = new List<AlphaControl>();
                    }
                    return _alphaControls;
                }
                set => _alphaControls = value;
            }
            private  List<AlphaControl> _alphaControls;

            /// <summary>
            /// Size Control 控制点列表
            /// </summary>
            [JsonProperty("sizeControl")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("sizeControl")]
#endif
            public List<SizeControl> SizeControls
            {
                get
                {
                    if (_sizeControls == null)
                    {
                        _sizeControls = new List<SizeControl>();
                    }
                    return _sizeControls;
                }
                set => _sizeControls = value;
            }
            private List<SizeControl> _sizeControls;

            /// <summary>
            /// Skew Control 控制点列表
            /// </summary>
            [JsonProperty("skewControl")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("skewControl")]
#endif
            public List<SkewControl> SkewControls
            {
                get
                {
                    if (_skewControls == null)
                    {
                        _skewControls = new List<SkewControl>();
                    }
                    return _skewControls;
                }
                set => _skewControls = value;
            }
            private List<SkewControl> _skewControls;

            /// <summary>
            /// Y Control 控制点列表
            /// </summary>
            [JsonProperty("yControl")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("yControl")]
#endif
            public List<YControl> YControls
            {
                get
                {
                    if (_yControls == null)
                    {
                        _yControls = new List<YControl>();
                    }
                    return _yControls;
                }
                set => _yControls = value;
            }
            private List<YControl> _yControls;

           public JudgeLine Clone()
           {
               var clone = new JudgeLine
               {
                   Name = Name,
                   Texture = Texture,
                   Anchor = (float[])Anchor.Clone(),
                   Father = Father,
                   IsCover = IsCover,
                   ZOrder = ZOrder,
                   IsGif = IsGif,
                   Group = Group,
                   BpmFactor = BpmFactor,
                   RotateWithFather = RotateWithFather,
                   AttachUi = AttachUi,
                   EventLayers = new List<EventLayer>(),
                   Notes = new List<Note>(),
                   Extended = Extended?.Clone(),
                   PositionControls = new List<XControl>(),
                   AlphaControls = new List<AlphaControl>(),
                   SizeControls = new List<SizeControl>(),
                   SkewControls = new List<SkewControl>(),
                   YControls = new List<YControl>()
               };
           
               // 深拷贝列表
               foreach (var eventLayer in EventLayers)
                   clone.EventLayers.Add(eventLayer.Clone());
               foreach (var note in Notes)
                   clone.Notes.Add(note.Clone());
               foreach (var control in PositionControls)
                   clone.PositionControls.Add(control.Clone() as XControl);
               foreach (var control in AlphaControls)
                   clone.AlphaControls.Add(control.Clone() as AlphaControl);
               foreach (var control in SizeControls)
                   clone.SizeControls.Add(control.Clone() as SizeControl);
               foreach (var control in SkewControls)
                   clone.SkewControls.Add(control.Clone() as SkewControl);
               foreach (var control in YControls)
                   clone.YControls.Add(control.Clone() as YControl);
           
               return clone;
           }
        }

        /// <summary>
        /// 绑定UI枚举
        /// </summary>
        public enum AttachUi
        {
            Pause = 1,
            ComboNumber = 2,
            Combo = 3,
            Score = 4,
            Bar = 5,
            Name = 6,
            Level = 7
        }
        
    }
}