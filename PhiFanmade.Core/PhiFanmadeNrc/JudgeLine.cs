using System.Collections.Generic;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public class JudgeLine
    {
        /// <summary>
        /// 判定线名称
        /// </summary>
        public string Name = "NrcJudgeLine";

        /// <summary>
        /// 判定线纹理相对路径，默认值为line.png
        /// </summary>
        public string Texture = "line.png"; // 判定线纹理路径

        /// <summary>
        /// 判定线纹理锚点(0~1之间)，默认值为中心点(0.5, 0.5)
        /// </summary>
        public float[] Anchor = { 0.5f, 0.5f }; // 判定线纹理锚点

        /// <summary>
        /// 判定线事件层列表
        /// </summary>
        public List<EventLayer> EventLayers = new List<EventLayer>(); // 事件层

        /// <summary>
        /// 父级判定线索引，-1表示无父级
        /// </summary>
        public int Father = -1; // 父级

        /// <summary>
        /// 是否遮罩越过判定线的音符（已被打击的除外）
        /// </summary>
        public bool IsCover = true; // 是否遮罩

        /// <summary>
        /// 判定线音符列表
        /// </summary>
        public List<Note> Notes = new List<Note>();

        /// <summary>
        /// 特殊事件层（故事板）
        /// </summary>
        public ExtendLayer Extended = new ExtendLayer();

        /// <summary>
        /// 判定线的Z轴顺序
        /// </summary>
        public int ZOrder; // Z轴顺序

        /// <summary>
        /// 判定线是否绑定UI
        /// </summary>
        public AttachUi? AttachUi = null; // 绑定UI名，当不绑定时为null

        /// <summary>
        /// 判定线纹理是否为GIF
        /// </summary>
        public bool IsGif; // 纹理是否为GIF

        /// <summary>
        /// 当前判定线相对于当前BPM的因子。判定线BPM = 谱面BPM / BpmFactor
        /// </summary>
        public float BpmFactor = 1.0f; // BPM因子

        /// <summary>
        /// 是否跟随父线旋转
        /// </summary>
        public bool RotateWithFather = false; // 是否随父级旋转

        /// <summary>
        /// Position（X） Control 控制点列表
        /// </summary>
        public List<XControl> PositionControls
        {
            get
            {
                _positionControls ??= new List<XControl>();

                return _positionControls;
            }
            set => _positionControls = value;
        }

        private List<XControl> _positionControls;

        /// <summary>
        /// Alpha Control 控制点列表
        /// </summary>
        public List<AlphaControl> AlphaControls
        {
            get
            {
                _alphaControls ??= new List<AlphaControl>();

                return _alphaControls;
            }
            set => _alphaControls = value;
        }

        private List<AlphaControl> _alphaControls;

        /// <summary>
        /// Size Control 控制点列表
        /// </summary>
        public List<SizeControl> SizeControls
        {
            get
            {
                _sizeControls ??= new List<SizeControl>();

                return _sizeControls;
            }
            set => _sizeControls = value;
        }

        private List<SizeControl> _sizeControls;

        /// <summary>
        /// Skew Control 控制点列表
        /// </summary>
        public List<SkewControl> SkewControls
        {
            get
            {
                _skewControls ??= new List<SkewControl>();

                return _skewControls;
            }
            set => _skewControls = value;
        }

        private List<SkewControl> _skewControls;

        /// <summary>
        /// Y Control 控制点列表
        /// </summary>
        public List<YControl> YControls
        {
            get
            {
                _yControls ??= new List<YControl>();

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
}