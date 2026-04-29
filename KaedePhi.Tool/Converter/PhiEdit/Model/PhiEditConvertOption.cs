namespace KaedePhi.Tool.Converter.PhiEdit.Model;

public class PhiEditConvertOptions
{
    public const double DefaultPrecision = 64d;
    public const double DefaultTolerancePercent = 0.1d;

    /// <summary>
    /// 事件切割相关配置
    /// </summary>
    public CuttingOptions Cutting { get; } = new();

    /// <summary>
    /// Alpha 事件相关配置
    /// </summary>
    public AlphaOptions Alpha { get; } = new();

    /// <summary>
    /// 速度事件相关配置
    /// </summary>
    public SpeedOptions Speed { get; } = new();

    /// <summary>
    /// 父子线解绑相关配置
    /// </summary>
    public FatherLineUnbindOptions FatherLineUnbind { get; } = new();

    /// <summary>
    /// 多层级合并相关配置
    /// </summary>
    public MultiLayerMergeOptions MultiLayerMerge { get; } = new();

    /// <summary>
    /// 判定线过滤相关配置
    /// </summary>
    public LineFilterOptions LineFilter { get; } = new();

    public class CuttingOptions
    {
        /// <summary>
        /// 非支持缓动切割精度
        /// </summary>
        public double UnsupportedEasingPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 非对齐 XY 事件切割精度
        /// </summary>
        public double MisalignedXyEventPrecision { get; set; } = DefaultPrecision;
    }

    public class AlphaOptions
    {
        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割精度
        /// </summary>
        public double CutPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割后是否压缩
        /// </summary>
        public bool CutCompress { get; set; } = true;

        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割后压缩容差百分比
        /// </summary>
        public double CutTolerance { get; set; } = DefaultTolerancePercent;
    }

    public class SpeedOptions
    {
        /// <summary>
        /// 速度事件切割精度
        /// </summary>
        public double CutPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 速度事件切割后是否压缩
        /// </summary>
        public bool CutCompress { get; set; } = true;

        /// <summary>
        /// 速度事件切割后压缩拟合容差百分比
        /// </summary>
        public double CutTolerance { get; set; } = DefaultTolerancePercent;
    }

    public class FatherLineUnbindOptions
    {
        private bool _classicMode = true;
        private bool _compress = false;

        /// <summary>
        /// 遇到父子线时父线解绑精度
        /// </summary>
        public double Precision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 遇到父子线时是否使用经典模式。
        /// 当 Compress 为 false 时，该值会被强制为 true。
        /// </summary>
        public bool ClassicMode
        {
            get => _classicMode;
            set
            {
                _classicMode = value;
                if (!_classicMode && !_compress)
                {
                    _compress = true;
                }
            }
        }

        /// <summary>
        /// 遇到父子线时合并后压缩拟合容差百分比
        /// </summary>
        public double Tolerance { get; set; } = 0.1d;

        /// <summary>
        /// 在启用经典模式的情况下，是否对解绑后的事件列表进行压缩
        /// 当该值为 false 时，ClassicMode 会被强制为 true。
        /// </summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                _compress = value;
                if (!_compress)
                {
                    _classicMode = true;
                }
            }
        }
    }

    public class MultiLayerMergeOptions
    {
        private bool _classicMode = true;
        private bool _compress = false;

        /// <summary>
        /// 遇到多层级时的合并精度
        /// </summary>
        public double Precision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 遇到多层级时合并后压缩拟合容差百分比
        /// </summary>
        public double Tolerance { get; set; } = 0.1d;

        /// <summary>
        /// 遇到多层级时是否使用经典模式。
        /// 当 Compress 为 false 时，该值会被强制为 true。
        /// </summary>
        public bool ClassicMode
        {
            get => _classicMode;
            set
            {
                _classicMode = value;
                if (!_classicMode && !_compress)
                {
                    _compress = true;
                }
            }
        }

        /// <summary>
        /// 在启用经典模式的情况下，是否对合并层级后的事件列表进行压缩
        /// 当该值为 false 时，ClassicMode 会被强制为 true。
        /// </summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                _compress = value;
                if (!_compress)
                {
                    _classicMode = true;
                }
            }
        }
    }

    public class LineFilterOptions
    {
        /// <summary>
        /// 是否删除带有绑定 UI 的判定线
        /// </summary>
        public bool RemoveAttachUiLine { get; set; }

        /// <summary>
        /// 是否移除带有自定义材质的判定线
        /// </summary>
        public bool RemoveTextureLine { get; set; }
    }
}