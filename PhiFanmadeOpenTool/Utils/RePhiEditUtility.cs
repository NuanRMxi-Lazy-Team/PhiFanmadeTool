using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;

namespace PhiFanmade.OpenTool.Utils;

public static class RePhiEditHelper
{
    public static class CoordinateTransform
    {
        public static float ToPhiEditX(float rePhiEditX)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinX;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxX;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinX;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxX;
            return (rePhiEditX - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }

        public static float ToPhiEditY(float rePhiEditY)
        {
            var rpeMin = RePhiEdit.Chart.CoordinateSystem.MinY;
            var rpeMax = RePhiEdit.Chart.CoordinateSystem.MaxY;
            var peMin = PhiEdit.Chart.CoordinateSystem.MinY;
            var peMax = PhiEdit.Chart.CoordinateSystem.MaxY;
            return (rePhiEditY - rpeMin) / (rpeMax - rpeMin) * (peMax - peMin) + peMin;
        }
    }

    public static Action<string> OnInfo = s => { };
    public static Action<string> OnWarning = s => { };
    public static Action<string> OnError = s => { };
    public static Action<string> OnDebug = s => { };

    /// <summary>
    /// 在有父线的情况下，获得一条判定线的绝对位置
    /// </summary>
    /// <param name="fatherLineX">父线X轴坐标</param>
    /// <param name="fatherLineY">父线Y轴坐标</param>
    /// <param name="angleDegrees">父线旋转角度</param>
    /// <param name="lineX">当前线相对于父线的X轴坐标</param>
    /// <param name="lineY">当前线相对于父线的Y轴坐标</param>
    /// <returns>当前线绝对坐标</returns>
    public static (double, double) GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        return RePhiEditUtility.FatherUnbindProcessor.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);
    }


    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static RePhiEdit.JudgeLine FatherUnbind(int targetJudgeLineIndex, List<RePhiEdit.JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
    {
        return RePhiEditUtility.FatherUnbindProcessor.FatherUnbind(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance);
    }

    public static List<RePhiEdit.Event<float>> EventListCompress(List<RePhiEdit.Event<float>> events,
        double tolerance = 5)
    {
        return RePhiEditUtility.EventProcessor.EventListCompress(events, tolerance);
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static async Task<RePhiEdit.JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<RePhiEdit.JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        return await RePhiEditUtility.FatherUnbindAsyncProcessor.FatherUnbindAsync(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance);
    }

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <typeparam name="T">呃</typeparam>
    /// <returns>已合并的事件列表</returns>
    public static List<RePhiEdit.Event<T>> EventMerge<T>(
        List<RePhiEdit.Event<T>> toEvents, List<RePhiEdit.Event<T>> formEvents, double precision = 64d,
        double tolerance = 5d)
        => RePhiEditUtility.EventProcessor.EventMerge(toEvents, formEvents, precision, tolerance);


    public static RePhiEdit.EventLayer LayerMerge(List<RePhiEdit.EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        return RePhiEditUtility.LayerProcessor.LayerMerge(layers, precision, tolerance);
    }
}