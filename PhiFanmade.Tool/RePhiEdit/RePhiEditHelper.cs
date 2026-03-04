using PhiFanmade.Core.RePhiEdit;
using PhiFanmade.Tool.RePhiEdit.Internal;

namespace PhiFanmade.Tool.RePhiEdit;

public static class RePhiEditHelper
{
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
        return FatherUnbindProcessor.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision"></param>
    /// <param name="tolerance"></param>
    /// <returns>解绑后结果</returns>
    public static JudgeLine FatherUnbind(int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
    {
        return FatherUnbindProcessor.FatherUnbind(targetJudgeLineIndex, allJudgeLines, precision, tolerance);
    }

    public static List<Event<float>> EventListCompress(List<Event<float>> events, double tolerance = 5)
    {
        return EventProcessor.EventListCompress(events, tolerance);
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static async Task<JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        return await FatherUnbindAsyncProcessor.FatherUnbindAsync(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance);
    }
    
    public static async Task<JudgeLine> FatherUnbindPlusAsync(int targetJudgeLineIndex,List<JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d)
    {
        return await FatherUnbindAsyncProcessor.FatherUnbindAsyncPlus(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance);
    }

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <param name="precision">切割精细度</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>已合并的事件列表</returns>
    public static List<Event<T>> EventMerge<T>(
        List<Event<T>> toEvents, List<Event<T>> formEvents, double precision = 64d,
        double tolerance = 5d)
        => EventProcessor.EventMerge(toEvents, formEvents, precision, tolerance);

    /// <summary>
    /// 更简易的事件列表合并方法，也许更省性能
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <param name="precision">切割精细度</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>已合并的事件列表</returns>
    public static List<Event<T>> EventMergePlus<T>(List<Event<T>> toEvents, List<Event<T>> formEvents,
        double precision = 64d, double tolerance = 5d)
        => EventProcessor.EventMergePlus(toEvents, formEvents, precision, tolerance);

    public static EventLayer LayerMerge(List<EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        return LayerProcessor.LayerMerge(layers, precision, tolerance);
    }
    
    public static EventLayer LayerMergePlus(List<EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        return LayerProcessor.LayerMergePlus(layers, precision, tolerance);
    }
}