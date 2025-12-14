using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class JudgeLine
        {
            /// <summary>
            /// 判定线名称
            /// </summary>
            [JsonProperty("Name")] public string Name = "PhiFanmadeCoreJudgeLine";

            /// <summary>
            /// 判定线纹理相对路径，默认值为line.png
            /// </summary>
            [JsonProperty("Texture")] public string Texture = "line.png"; // 判定线纹理路径

            /// <summary>
            /// 判定线纹理锚点(0~1之间)，默认值为中心点(0.5, 0.5)
            /// </summary>
            [JsonProperty("anchor")] public float[] Anchor = { 0.5f, 0.5f }; // 判定线纹理锚点

            /// <summary>
            /// 判定线事件层列表
            /// </summary>
            [JsonProperty("eventLayers", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<EventLayer> EventLayers = new List<EventLayer>(); // 事件层

            /// <summary>
            /// 父级判定线索引，-1表示无父级
            /// </summary>
            [JsonProperty("father")] public int Father = -1; // 父级

            /// <summary>
            /// 是否遮罩越过判定线的音符（已被打击的除外）
            /// </summary>
            [JsonProperty("isCover")] [JsonConverter(typeof(BoolConverter))]
            public bool IsCover = true; // 是否遮罩

            /// <summary>
            /// 判定线音符列表
            /// </summary>
            [JsonProperty("notes")] public List<Note> Notes = new List<Note>(); // note列表

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
            public ExtendLayer Extended = new ExtendLayer();

            /// <summary>
            /// 判定线的Z轴顺序
            /// </summary>
            [JsonProperty("zOrder")] public int ZOrder; // Z轴顺序

            /// <summary>
            /// 判定线是否绑定UI
            /// </summary>
            [JsonProperty("attachUI", NullValueHandling = NullValueHandling.Ignore)]
            [JsonConverter(typeof(AttachUiConverter))]
            public AttachUi? AttachUi = null; // 绑定UI名，当不绑定时为null

            /// <summary>
            /// 判定线纹理是否为GIF
            /// </summary>
            [JsonProperty("isGif")] public bool IsGif; // 纹理是否为GIF

            /// <summary>
            /// 所属组
            /// </summary>
            [JsonProperty("Group")] public int Group = 0; // 绑定组

            /// <summary>
            /// 当前判定线相对于当前BPM的因子。判定线BPM = 谱面BPM / BpmFactor
            /// </summary>
            [JsonProperty("bpmfactor")] public float BpmFactor = 1.0f; // BPM因子

            /// <summary>
            /// 是否跟随父线旋转
            /// </summary>
            [JsonProperty("rotateWithFather")] public bool RotateWithFather = false; // 是否随父级旋转

            /// <summary>
            /// Position（X） Control 控制点列表
            /// </summary>
            [JsonProperty("posControl")]
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
            [JsonProperty("yControl")] public List<YControl> YControls
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