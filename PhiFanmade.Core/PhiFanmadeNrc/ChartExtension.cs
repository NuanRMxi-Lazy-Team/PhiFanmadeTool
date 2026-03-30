namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public partial class Chart
    {
        /// <summary>
        /// 对判定线及其事件层级进行预处理。
        /// </summary>
        public void Anticipation()
        {
            foreach (var judgeLine in JudgeLineList)
            {
                // 如果这个判定线层级上有null层级，移除它们
                judgeLine.EventLayers.RemoveAll(layer => layer == null);
                // 对所有判定线的所有事件层级执行Anticipation()方法
                foreach (var eventLayer in judgeLine.EventLayers)
                {
                    eventLayer.Anticipation();
                    eventLayer.Sort();
                }

                judgeLine.Extended.Anticipation();
                // 如果判定线上有任何类型的Control组为空或null，则设定一个默认值
                if (judgeLine.AlphaControls == null || judgeLine.AlphaControls.Count == 0)
                    judgeLine.AlphaControls = AlphaControl.Default;
                if (judgeLine.PositionControls == null || judgeLine.PositionControls.Count == 0)
                    judgeLine.PositionControls = XControl.Default;
                if (judgeLine.SizeControls == null || judgeLine.SizeControls.Count == 0)
                    judgeLine.SizeControls = SizeControl.Default;
                if (judgeLine.SkewControls == null || judgeLine.SkewControls.Count == 0)
                    judgeLine.SkewControls = SkewControl.Default;
                if (judgeLine.YControls == null || judgeLine.YControls.Count == 0)
                    judgeLine.YControls = YControl.Default;
            }
        }
        
        public Chart Clone()
        {
            return new Chart
            {
                BpmList = BpmList.ConvertAll(bpm => bpm.Clone()),
                Meta = Meta.Clone(),
                JudgeLineList = JudgeLineList.ConvertAll(judgeLine => judgeLine.Clone()),
            };
        }
    }
}