using PhiFanmade.Core.PhiEdit;
using PhiFanmade.Core.RePhiEdit;
using PhiFanmade.OpenTool.Localization;
using System.Collections.Concurrent;

namespace PhiFanmade.OpenTool.Utils;

public class RePhiEditUtility
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
    public static Tuple<double, double> GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        // 将角度转换为弧度
        double angleRadians = (angleDegrees % 360) * Math.PI / 180f;

        // 计算旋转后的坐标
        double rotatedX = lineX * Math.Cos(angleRadians) + lineY * Math.Sin(angleRadians);
        double rotatedY = -lineX * Math.Sin(angleRadians) + lineY * Math.Cos(angleRadians);

        // 计算绝对坐标
        double absoluteX = fatherLineX + rotatedX;
        double absoluteY = fatherLineY + rotatedY;

        return new(absoluteX, absoluteY);
    }


    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static RePhiEdit.JudgeLine FatherUnbind(int targetJudgeLineIndex, List<RePhiEdit.JudgeLine> allJudgeLines)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
                fatherLineCopy = FatherUnbind(fatherLineCopy.Father, allJudgeLinesCopy);
            // 合并judgeLineCopy的所有层级的XEvent和YEvent
            var targetLineNewXevents = new List<RePhiEdit.Event<float>>();
            var targetLineNewYevents = new List<RePhiEdit.Event<float>>();
            foreach (var layer in judgeLineCopy.EventLayers)
            {
                targetLineNewXevents = EventMerge(targetLineNewXevents, layer.MoveXEvents);
                targetLineNewYevents = EventMerge(targetLineNewYevents, layer.MoveYEvents);
            }

            // 合并fatherLineCopy的所有层级的XEvent和YEvent，额外合并RotateEvents
            var fatherLineNewXevents = new List<RePhiEdit.Event<float>>();
            var fatherLineNewYevents = new List<RePhiEdit.Event<float>>();
            var fatherLineNewRotateEvents = new List<RePhiEdit.Event<float>>();
            foreach (var layer in fatherLineCopy.EventLayers)
            {
                fatherLineNewXevents = EventMerge(fatherLineNewXevents, layer.MoveXEvents);
                fatherLineNewYevents = EventMerge(fatherLineNewYevents, layer.MoveYEvents);
                fatherLineNewRotateEvents = EventMerge(fatherLineNewRotateEvents, layer.RotateEvents);
            }

            // 全部Cut，从头Cut到尾，调用CutEventsInRange方法
            // 得到targetLineNewXevents的最小开始拍，使用LINQ
            var targetLineXEventsMinBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var targetLineXEventsMaxBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            // YEvents
            var targetLineYEventsMinBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var targetLineYEventsMaxBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            targetLineNewXevents =
                CutEventsInRange(targetLineNewXevents, targetLineXEventsMinBeat!, targetLineXEventsMaxBeat!);
            targetLineNewYevents =
                CutEventsInRange(targetLineNewYevents, targetLineYEventsMinBeat!, targetLineYEventsMaxBeat!);
            // 然后是fatherLineNewXevents
            var fatherLineXEventsMinBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineXEventsMaxBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            // YEvents
            var fatherLineYEventsMinBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineYEventsMaxBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            // RotateEvents
            var fatherLineRotateEventsMinBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineRotateEventsMaxBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            fatherLineNewXevents =
                CutEventsInRange(fatherLineNewXevents, fatherLineXEventsMinBeat!, fatherLineXEventsMaxBeat!);
            fatherLineNewYevents =
                CutEventsInRange(fatherLineNewYevents, fatherLineYEventsMinBeat!, fatherLineYEventsMaxBeat!);
            fatherLineNewRotateEvents =
                CutEventsInRange(fatherLineNewRotateEvents, fatherLineRotateEventsMinBeat!,
                    fatherLineRotateEventsMaxBeat!);
            var previousFatherXValue = 0.0f;
            var previousFatherYValue = 0.0f;
            var previousFatherRotateValue = 0.0f;
            var previousTargetXValue = 0.0f;
            var previousTargetYValue = 0.0f;
            // minBeat在targetLineXEventsMinBeat targetLineYEventsMinBeat fatherLineXEventsMinBeat fatherLineYEventsMinBeat中最小的
            var overallMinBeat = new RePhiEdit.Beat(Math.Min(
                Math.Min(targetLineXEventsMinBeat!, targetLineYEventsMinBeat!),
                Math.Min(fatherLineXEventsMinBeat!, fatherLineYEventsMinBeat!)));
            // maxBeat在targetLineXEventsMaxBeat targetLineYEventsMaxBeat fatherLineXEventsMaxBeat fatherLineYEventsMaxBeat中最大的
            var overallMaxBeat = new RePhiEdit.Beat(Math.Max(
                Math.Max(targetLineXEventsMaxBeat!, targetLineYEventsMaxBeat!),
                Math.Max(fatherLineXEventsMaxBeat!, fatherLineYEventsMaxBeat!)));
            var cutLength = new RePhiEdit.Beat(1d / 64d); // 1/64拍
            var currentBeat = overallMinBeat;
            var targetLineXEventResult = new List<RePhiEdit.Event<float>>();
            var targetLineYEventResult = new List<RePhiEdit.Event<float>>();
            while (currentBeat <= overallMaxBeat)
            {
                var nextBeat = currentBeat + cutLength;
                // 获得fatherLine在currentBeat的X和Y值，也有可能没有事件，则使用previousFather*EndValue，事件的StartBeat需要等于currentBeat，EndBeat需要等于currentBeat + cutLength
                var fatherXevent =
                    fatherLineNewXevents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                var fatherYevent =
                    fatherLineNewYevents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                var fatherXStartValue = fatherXevent != null ? fatherXevent.StartValue : previousFatherXValue;
                var fatherYStartValue = fatherYevent != null ? fatherYevent.StartValue : previousFatherYValue;
                var fatherXEndValue = fatherXevent != null ? fatherXevent.EndValue : previousFatherXValue;
                var fatherYEndValue = fatherYevent != null ? fatherYevent.EndValue : previousFatherYValue;
                // 获得fatherLine在currentBeat的Rotate值
                var fatherRotateEvent =
                    fatherLineNewRotateEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                var fatherRotateStartValue =
                    fatherRotateEvent != null ? fatherRotateEvent.StartValue : previousFatherRotateValue;
                var fatherRotateEndValue =
                    fatherRotateEvent != null ? fatherRotateEvent.EndValue : previousFatherRotateValue;
                // 获得targetLine在currentBeat的X和Y值，也有可能没有事件，则使用previousTarget*EndValue
                var targetXevent =
                    targetLineNewXevents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                var targetYevent =
                    targetLineNewYevents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                var targetXStartValue = targetXevent != null ? targetXevent.StartValue : previousTargetXValue;
                var targetYStartValue = targetYevent != null ? targetYevent.StartValue : previousTargetYValue;
                var targetXEndValue = targetXevent != null ? targetXevent.EndValue : previousTargetXValue;
                var targetYEndValue = targetYevent != null ? targetYevent.EndValue : previousTargetYValue;
                // 统一更新prev
                previousFatherXValue = fatherXEndValue;
                previousFatherYValue = fatherYEndValue;
                previousFatherRotateValue = fatherRotateEndValue;
                previousTargetXValue = targetXEndValue;
                previousTargetYValue = targetYEndValue;
                // 计算绝对位置
                var (absStartX, absStartY) = GetLinePos(fatherXStartValue, fatherYStartValue,
                    fatherRotateStartValue, targetXStartValue, targetYStartValue);
                var (absEndX, absEndY) = GetLinePos(fatherXEndValue, fatherYEndValue,
                    fatherRotateEndValue, targetXEndValue, targetYEndValue);
                // 新建事件，长度为cutLength，起始拍为currentBeat，结束拍为currentBeat + cutLength
                var newXEvent = new RePhiEdit.Event<float>
                {
                    StartBeat = currentBeat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartX,
                    EndValue = (float)absEndX,
                };
                var newYEvnt = new RePhiEdit.Event<float>
                {
                    StartBeat = currentBeat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartY,
                    EndValue = (float)absEndY,
                };
                // 添加到结果列表
                targetLineXEventResult.Add(newXEvent);
                targetLineYEventResult.Add(newYEvnt);
                currentBeat += cutLength;
            }

            // 清除judgeLineCopy其它层级的XEvent和YEvent，保留第一个层级，然后将计算得到的结果赋值给第一个层级
            for (var i = 1; i < judgeLineCopy.EventLayers.Count; i++)
            {
                judgeLineCopy.EventLayers[i].MoveXEvents.Clear();
                judgeLineCopy.EventLayers[i].MoveYEvents.Clear();
            }

            if (judgeLineCopy.EventLayers.Count == 0)
            {
                judgeLineCopy.EventLayers.Add(new RePhiEdit.EventLayer());
            }

            judgeLineCopy.EventLayers[0].MoveXEvents = targetLineXEventResult;
            judgeLineCopy.EventLayers[0].MoveYEvents = targetLineYEventResult;
            // 还有RotateWithFather字段，此字段为true的时候，需要给targetLineCopy累加父线的Rotate事件
            if (judgeLineCopy.RotateWithFather)
            {
                // 把fatherLineNewRotateEvents合并给judgeLineCopy的第一个层级
                judgeLineCopy.EventLayers[0].RotateEvents =
                    EventMerge(judgeLineCopy.EventLayers[0].RotateEvents, fatherLineNewRotateEvents);
            }


            // 解绑father
            judgeLineCopy.Father = -1;
            return judgeLineCopy;
        }
        catch (NullReferenceException)
        {
            OnWarning.Invoke("FatherUnbind: It seems that something is null.");
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            OnWarning.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }

    public static List<RePhiEdit.Event<float>> EventListCompress(List<RePhiEdit.Event<float>> events,
        double tolerance = 5)
    {
        if (events == null || events.Count == 0)
            return new List<RePhiEdit.Event<float>>();

        var compressed = new List<RePhiEdit.Event<float>> { events[0] };

        for (int i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            // 两个事件必须为线性事件，且两个事件数值变化率相同，且结束拍起始拍相连，且结束数值起始数值相等
            if (lastEvent.Easing == 1 && currentEvent.Easing == 1)
            {
                var lastRate = (lastEvent.EndValue - lastEvent.StartValue) /
                               (lastEvent.EndBeat - lastEvent.StartBeat);
                var currentRate = (currentEvent.EndValue - currentEvent.StartValue) /
                                  (currentEvent.EndBeat - currentEvent.StartBeat);

                if (Math.Abs(lastRate - currentRate) < tolerance &&
                    lastEvent.EndBeat == currentEvent.StartBeat &&
                    Math.Abs(lastEvent.EndValue - currentEvent.StartValue) < tolerance)
                {
                    // 合并事件
                    lastEvent.EndBeat = currentEvent.EndBeat;
                    lastEvent.EndValue = currentEvent.EndValue;
                    continue;
                }
            }

            // 无法合并时，添加当前事件
            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <returns></returns>
    public static async Task<RePhiEdit.JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<RePhiEdit.JudgeLine> allJudgeLines)
    {
        return await Task.Run(() => FatherUnbindCore(targetJudgeLineIndex, allJudgeLines));
    }

    private static RePhiEdit.JudgeLine FatherUnbindCore(int targetJudgeLineIndex,
        List<RePhiEdit.JudgeLine> allJudgeLines)
    {
        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                OnWarning.Invoke("FatherUnbind: judgeLine has no father.");
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
                fatherLineCopy = FatherUnbindCore(fatherLineCopy.Father, allJudgeLinesCopy);

            // 并行合并事件层级前,先移除无用层级
            judgeLineCopy.EventLayers = RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;
            // 并行合并事件层级
            var targetLineNewXevents = new List<RePhiEdit.Event<float>>();
            var targetLineNewYevents = new List<RePhiEdit.Event<float>>();
            var fatherLineNewXevents = new List<RePhiEdit.Event<float>>();
            var fatherLineNewYevents = new List<RePhiEdit.Event<float>>();
            var fatherLineNewRotateEvents = new List<RePhiEdit.Event<float>>();

            // 使用 ConcurrentBag 来收集并行处理的结果
            var targetXBag = new ConcurrentBag<List<RePhiEdit.Event<float>>>();
            var targetYBag = new ConcurrentBag<List<RePhiEdit.Event<float>>>();

            Parallel.ForEach(judgeLineCopy.EventLayers, layer =>
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    targetXBag.Add(layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    targetYBag.Add(layer.MoveYEvents);
            });

            foreach (var events in targetXBag)
                targetLineNewXevents = EventMerge(targetLineNewXevents, events);
            foreach (var events in targetYBag)
                targetLineNewYevents = EventMerge(targetLineNewYevents, events);

            // 并行处理父线事件
            var fatherXBag = new ConcurrentBag<List<RePhiEdit.Event<float>>>();
            var fatherYBag = new ConcurrentBag<List<RePhiEdit.Event<float>>>();
            var fatherRotateBag = new ConcurrentBag<List<RePhiEdit.Event<float>>>();

            Parallel.ForEach(fatherLineCopy.EventLayers, layer =>
            {
                if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                    fatherXBag.Add(layer.MoveXEvents);
                if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                    fatherYBag.Add(layer.MoveYEvents);
                if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                    fatherRotateBag.Add(layer.RotateEvents);
            });

            foreach (var events in fatherXBag)
                fatherLineNewXevents = EventMerge(fatherLineNewXevents, events);
            foreach (var events in fatherYBag)
                fatherLineNewYevents = EventMerge(fatherLineNewYevents, events);
            foreach (var events in fatherRotateBag)
                fatherLineNewRotateEvents = EventMerge(fatherLineNewRotateEvents, events);

            // 计算拍范围
            var targetLineXEventsMinBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var targetLineXEventsMaxBeat = targetLineNewXevents.Count > 0
                ? targetLineNewXevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            var targetLineYEventsMinBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var targetLineYEventsMaxBeat = targetLineNewYevents.Count > 0
                ? targetLineNewYevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);

            var fatherLineXEventsMinBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineXEventsMaxBeat = fatherLineNewXevents.Count > 0
                ? fatherLineNewXevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineYEventsMinBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineYEventsMaxBeat = fatherLineNewYevents.Count > 0
                ? fatherLineNewYevents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineRotateEventsMinBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Min(e => e.StartBeat)
                : new RePhiEdit.Beat(0);
            var fatherLineRotateEventsMaxBeat = fatherLineNewRotateEvents.Count > 0
                ? fatherLineNewRotateEvents.Max(e => e.EndBeat)
                : new RePhiEdit.Beat(0);

            // 并行切割事件
            var cutTasks = new[]
            {
                Task.Run(() =>
                    CutEventsInRange(targetLineNewXevents, targetLineXEventsMinBeat!, targetLineXEventsMaxBeat!)),
                Task.Run(() =>
                    CutEventsInRange(targetLineNewYevents, targetLineYEventsMinBeat!, targetLineYEventsMaxBeat!)),
                Task.Run(() =>
                    CutEventsInRange(fatherLineNewXevents, fatherLineXEventsMinBeat!, fatherLineXEventsMaxBeat!)),
                Task.Run(() =>
                    CutEventsInRange(fatherLineNewYevents, fatherLineYEventsMinBeat!, fatherLineYEventsMaxBeat!)),
                Task.Run(() => CutEventsInRange(fatherLineNewRotateEvents, fatherLineRotateEventsMinBeat!,
                    fatherLineRotateEventsMaxBeat!))
            };

            Task.WaitAll(cutTasks);

            targetLineNewXevents = cutTasks[0].Result;
            targetLineNewYevents = cutTasks[1].Result;
            fatherLineNewXevents = cutTasks[2].Result;
            fatherLineNewYevents = cutTasks[3].Result;
            fatherLineNewRotateEvents = cutTasks[4].Result;
            /*
            // DEBUG START
            // 将targetLineNewXevents替换到judgeLineCopy的第一个层级的MoveXEvents、将targetLineNewYevents替换到judgeLineCopy的第一个层级的MoveYEvents，替换前进行精简
            judgeLineCopy.EventLayers[0].MoveXEvents = EventListCompress(targetLineNewXevents);
            judgeLineCopy.EventLayers[0].MoveYEvents = EventListCompress(targetLineNewYevents);
            // 直接return，调试
            return judgeLineCopy;
            // DEBUG END
            */

            var overallMinBeat = new RePhiEdit.Beat(Math.Min(
                Math.Min(targetLineXEventsMinBeat!, targetLineYEventsMinBeat!),
                Math.Min(fatherLineXEventsMinBeat!, fatherLineYEventsMinBeat!)));
            var overallMaxBeat = new RePhiEdit.Beat(Math.Max(
                Math.Max(targetLineXEventsMaxBeat!, targetLineYEventsMaxBeat!),
                Math.Max(fatherLineXEventsMaxBeat!, fatherLineYEventsMaxBeat!)));
            var cutLength = new RePhiEdit.Beat(1d / 64d);

            // 并行处理每个拍段
            var currentBeat = overallMinBeat;
            var beatSegments = new List<RePhiEdit.Beat>();
            while (currentBeat <= overallMaxBeat)
            {
                beatSegments.Add(currentBeat);
                currentBeat += cutLength;
            }

            var targetLineXEventResult = new ConcurrentBag<(int index, RePhiEdit.Event<float> evt)>();
            var targetLineYEventResult = new ConcurrentBag<(int index, RePhiEdit.Event<float> evt)>();

            Parallel.For(0, beatSegments.Count, i =>
            {
                var beat = beatSegments[i];
                var nextBeat = beat + cutLength;

                var previousFatherXValue = 0.0f;
                var previousFatherYValue = 0.0f;
                var previousFatherRotateValue = 0.0f;
                var previousTargetXValue = 0.0f;
                var previousTargetYValue = 0.0f;

                // 获取之前的值
                if (i > 0)
                {
                    var prevFatherX = fatherLineNewXevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevFatherY = fatherLineNewYevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevFatherRotate = fatherLineNewRotateEvents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevTargetX = targetLineNewXevents.LastOrDefault(e => e.EndBeat <= beat);
                    var prevTargetY = targetLineNewYevents.LastOrDefault(e => e.EndBeat <= beat);

                    previousFatherXValue = prevFatherX?.EndValue ?? 0.0f;
                    previousFatherYValue = prevFatherY?.EndValue ?? 0.0f;
                    previousFatherRotateValue = prevFatherRotate?.EndValue ?? 0.0f;
                    previousTargetXValue = prevTargetX?.EndValue ?? 0.0f;
                    previousTargetYValue = prevTargetY?.EndValue ?? 0.0f;
                }

                var fatherXevent =
                    fatherLineNewXevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherYevent =
                    fatherLineNewYevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherXStartValue = fatherXevent != null ? fatherXevent.StartValue : previousFatherXValue;
                var fatherYStartValue = fatherYevent != null ? fatherYevent.StartValue : previousFatherYValue;
                var fatherXEndValue = fatherXevent != null ? fatherXevent.EndValue : previousFatherXValue;
                var fatherYEndValue = fatherYevent != null ? fatherYevent.EndValue : previousFatherYValue;

                var fatherRotateEvent =
                    fatherLineNewRotateEvents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var fatherRotateStartValue =
                    fatherRotateEvent != null ? fatherRotateEvent.StartValue : previousFatherRotateValue;
                var fatherRotateEndValue =
                    fatherRotateEvent != null ? fatherRotateEvent.EndValue : previousFatherRotateValue;

                var targetXevent =
                    targetLineNewXevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var targetYevent =
                    targetLineNewYevents.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == nextBeat);
                var targetXStartValue = targetXevent != null ? targetXevent.StartValue : previousTargetXValue;
                var targetYStartValue = targetYevent != null ? targetYevent.StartValue : previousTargetYValue;
                var targetXEndValue = targetXevent != null ? targetXevent.EndValue : previousTargetXValue;
                var targetYEndValue = targetYevent != null ? targetYevent.EndValue : previousTargetYValue;

                var (absStartX, absStartY) = GetLinePos(fatherXStartValue, fatherYStartValue,
                    fatherRotateStartValue, targetXStartValue, targetYStartValue);
                var (absEndX, absEndY) = GetLinePos(fatherXEndValue, fatherYEndValue,
                    fatherRotateEndValue, targetXEndValue, targetYEndValue);

                var newXEvent = new RePhiEdit.Event<float>
                {
                    StartBeat = beat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartX,
                    EndValue = (float)absEndX,
                };
                var newYEvent = new RePhiEdit.Event<float>
                {
                    StartBeat = beat,
                    EndBeat = nextBeat,
                    StartValue = (float)absStartY,
                    EndValue = (float)absEndY,
                };

                targetLineXEventResult.Add((i, newXEvent));
                targetLineYEventResult.Add((i, newYEvent));
            });

            // 排序并转换为列表
            var sortedXEvents = targetLineXEventResult.OrderBy(x => x.index).Select(x => x.evt).ToList();
            var sortedYEvents = targetLineYEventResult.OrderBy(x => x.index).Select(x => x.evt).ToList();

            // 清除其它层级
            for (var i = 1; i < judgeLineCopy.EventLayers.Count; i++)
            {
                judgeLineCopy.EventLayers[i].MoveXEvents.Clear();
                judgeLineCopy.EventLayers[i].MoveYEvents.Clear();
            }

            if (judgeLineCopy.EventLayers.Count == 0)
            {
                judgeLineCopy.EventLayers.Add(new RePhiEdit.EventLayer());
            }

            judgeLineCopy.EventLayers[0].MoveXEvents = EventListCompress(sortedXEvents);
            judgeLineCopy.EventLayers[0].MoveYEvents = EventListCompress(sortedYEvents);

            if (judgeLineCopy.RotateWithFather)
            {
                judgeLineCopy.EventLayers[0].RotateEvents =
                    EventListCompress(EventMerge(judgeLineCopy.EventLayers[0].RotateEvents, fatherLineNewRotateEvents));
            }

            judgeLineCopy.Father = -1;
            return judgeLineCopy;
        }
        catch (NullReferenceException)
        {
            OnWarning.Invoke("FatherUnbind: It seems that something is null.");
            return judgeLineCopy;
        }
        catch (Exception e)
        {
            OnWarning.Invoke("FatherUnbind: Unknown error: " + e.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 泛型加法辅助方法，避免在 AOT 中使用 dynamic
    /// </summary>
    private static T AddValues<T>(T a, T b)
    {
        if (typeof(T) == typeof(int))
            return (T)(object)((int)(object)a! + (int)(object)b!);
        if (typeof(T) == typeof(float))
            return (T)(object)((float)(object)a! + (float)(object)b!);
        if (typeof(T) == typeof(double))
            return (T)(object)((double)(object)a! + (double)(object)b!);
        throw new NotSupportedException($"Type {typeof(T)} is not supported for addition");
    }

    /// <summary>
    /// 在指定的拍范围内切割事件列表
    /// </summary>
    /// <param name="events">要切割的事件列表</param>
    /// <param name="startBeat">开始拍</param>
    /// <param name="endBeat">结束拍</param>
    /// <param name="cutLength">切割长度（默认0.015625拍）</param>
    /// <typeparam name="T">事件值类型</typeparam>
    /// <returns>切割后的事件列表</returns>
    private static List<RePhiEdit.Event<T>> CutEventsInRange<T>(
        List<RePhiEdit.Event<T>> events,
        RePhiEdit.Beat startBeat,
        RePhiEdit.Beat endBeat,
        RePhiEdit.Beat? cutLength = null)
    {
        var length = cutLength ?? new RePhiEdit.Beat(1d / 64d);
        var cutedEvents = new List<RePhiEdit.Event<T>>();

        // 找到在指定范围内的事件
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd = evt.EndBeat > endBeat ? endBeat : evt.EndBeat;

            // 计算需要切割的段数，避免浮点累加误差
            var totalBeats = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling(totalBeats / length);

            for (int i = 0; i < segmentCount; i++)
            {
                // 使用索引计算位置，而不是累加
                var currentBeat = new RePhiEdit.Beat(cutStart + (length * i));
                var segmentEnd = new RePhiEdit.Beat(cutStart + (length * (i + 1)));

                // 最后一段可能需要调整
                if (segmentEnd > cutEnd)
                    segmentEnd = cutEnd;

                var newEvent = new RePhiEdit.Event<T>
                {
                    StartBeat = currentBeat,
                    EndBeat = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue = evt.GetValueAtBeat(segmentEnd),
                };
                cutedEvents.Add(newEvent);
            }
        }

        return cutedEvents;
    }

    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <typeparam name="T">呃</typeparam>
    /// <returns>已合并的事件列表</returns>
    public static List<RePhiEdit.Event<T>> EventMerge<T>(
        List<RePhiEdit.Event<T>> toEvents, List<RePhiEdit.Event<T>> formEvents)
    {
        var loc = Localizer.Create();
        // 先检查 null，避免 LINQ "Value cannot be null. (Parameter 'source')" 错误
        if (toEvents == null || toEvents.Count == 0)
        {
            if (formEvents == null || formEvents.Count == 0)
                return new();
            return formEvents.Select(e => e.Clone()).ToList();
        }

        if (formEvents == null || formEvents.Count == 0)
        {
            return toEvents.Select(e => e.Clone()).ToList();
        }

        var toEventsCopy = toEvents.Select(e => e.Clone()).ToList();
        var formEventsCopy = formEvents.Select(e => e.Clone()).ToList();
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
        {
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
        }


        // 将formEvents合并进toEvents，先检查是否有重合事件
        var overlapFound = false;
        foreach (var formEvent in formEventsCopy)
        {
            foreach (var toEvent in toEventsCopy)
            {
                if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                {
                    OnWarning.Invoke(
                        $"EventMerge: Overlapping events detected between {formEvent.StartBeat}-{formEvent.EndBeat} and {toEvent.StartBeat}-{toEvent.EndBeat}.");
                    overlapFound = true;
                    break;
                }
            }

            if (overlapFound)
                break;
        }

        if (overlapFound)
        {
            var newEvents = new List<RePhiEdit.Event<T>>();
            // 获得所有重合区间，比如，formEvents在1~2、4~8拍有事件，toEvents在1~8拍有事件，则重合区间以较长的为准，即1~8拍
            var overlapIntervals = new List<(RePhiEdit.Beat Start, RePhiEdit.Beat End)>();
            foreach (var formEvent in formEventsCopy)
            {
                foreach (var toEvent in toEventsCopy)
                {
                    if (formEvent.StartBeat < toEvent.EndBeat && formEvent.EndBeat > toEvent.StartBeat)
                    {
                        var start = formEvent.StartBeat < toEvent.StartBeat ? formEvent.StartBeat : toEvent.StartBeat;
                        var end = formEvent.EndBeat > toEvent.EndBeat ? formEvent.EndBeat : toEvent.EndBeat;
                        // 如果已经存在，不要添加
                        if (overlapIntervals.Any(interval => interval.Start == start && interval.End == end))
                            continue;
                        // 如果与其它overlapInterval有重合，进行合并（扩展）
                        if (overlapIntervals.Any(interval =>
                                start < interval.End && end > interval.Start))
                        {
                            for (var i = 0; i < overlapIntervals.Count; i++)
                            {
                                var interval = overlapIntervals[i];
                                if (start < interval.End && end > interval.Start)
                                {
                                    var newStart = start < interval.Start ? start : interval.Start;
                                    var newEnd = end > interval.End ? end : interval.End;
                                    overlapIntervals[i] = (newStart, newEnd);
                                    start = newStart;
                                    end = newEnd;
                                }
                            }
                        }
                        else overlapIntervals.Add((start, end)); // 否则直接添加
                    }
                }
            }

            overlapIntervals.Sort((a, b) =>
            {
                if (a.Start < b.Start) return -1;
                if (a.Start > b.Start) return 1;
                if (a.End < b.End) return -1;
                if (a.End > b.End) return 1;
                return 0;
            });

            // 先把未重合的事件加入newEvents（注意需要加上另一侧最近结束值的偏移）
            foreach (var toEvent in toEventsCopy)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    toEvent.StartBeat < interval.End && toEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    // to-only 区间：应加上 formEvents 在该拍之前最近结束事件的结束值
                    var previousFormEvent = formEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat);
                    var formOffset = previousFormEvent != null ? previousFormEvent.EndValue : default;
                    var adjusted = new RePhiEdit.Event<T>
                    {
                        StartBeat = toEvent.StartBeat,
                        EndBeat = toEvent.EndBeat,
                        StartValue = AddValues(toEvent.StartValue!, formOffset!),
                        EndValue = AddValues(toEvent.EndValue!, formOffset!),
                        BezierPoints = toEvent.BezierPoints,
                        Easing = toEvent.Easing,
                        EasingLeft = toEvent.EasingLeft,
                        EasingRight = toEvent.EasingRight,
                        IsBezier = toEvent.IsBezier,
                    };
                    newEvents.Add(adjusted);
                }
            }

            // 由于当前数值是两个事件列表的数值相加得到的，所以未重合的formEvents事件需要加上toEvents在该拍的数值
            foreach (var formEvent in formEventsCopy)
            {
                var isInOverlap = overlapIntervals.Any(interval =>
                    formEvent.StartBeat < interval.End && formEvent.EndBeat > interval.Start);
                if (!isInOverlap)
                {
                    // 获得这个事件StartBeat前的第一个原始toEvents的结束值，而不是已修改的toEventsCopy
                    var previousToEvent = toEvents.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                    var toEventValue = previousToEvent != null ? previousToEvent.EndValue : (T)default;
                    // 创建新的事件对象而不是直接修改formEvent
                    var adjustedEvent = new RePhiEdit.Event<T>
                    {
                        StartBeat = formEvent.StartBeat,
                        EndBeat = formEvent.EndBeat,
                        StartValue = AddValues(formEvent.StartValue, toEventValue),
                        EndValue = AddValues(formEvent.EndValue, toEventValue),
                        BezierPoints = formEvent.BezierPoints,
                        Easing = formEvent.Easing,
                        EasingLeft = formEvent.EasingLeft,
                        EasingRight = formEvent.EasingRight,
                        IsBezier = formEvent.IsBezier,
                    };
                    newEvents.Add(adjustedEvent);
                }
            }


            // 对每个区间内的事件进行切割，两个事件列表都要做切割，切割后的事件长度为0.015625拍
            var cutLength = new RePhiEdit.Beat(0.015625d);
            var cutedToEvents = new List<RePhiEdit.Event<T>>();
            var cutedFormEvents = new List<RePhiEdit.Event<T>>();
            foreach (var (start, end) in overlapIntervals)
            {
                // 使用新方法切割toEvents内的事件
                var cutToInRange = CutEventsInRange(toEventsCopy, start, end, cutLength);
                cutedToEvents.AddRange(cutToInRange);

                // 使用新方法切割formEvents内的事件
                var cutFormInRange = CutEventsInRange(formEventsCopy, start, end, cutLength);
                cutedFormEvents.AddRange(cutFormInRange);

                // 从原列表中移除已切割的事件
                toEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
                formEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
            }

            // 再次合并，现在所有事件长度都一致了，但是要注意，两个事件列表的当前值总和为最终值，无事件的地方使用上一个事件的结束值，没有上一个事件则使用默认值，如果合并不当会导致数值跳变
            var allCutedEvents = new List<RePhiEdit.Event<T>>();
            var formOverlapEventLastEndValue = default(T);
            var toOverlapEventLastEndValue = default(T);
            // 以0.015625拍为采样大小，遍历每一个重合区间
            for (var index = 0; index < overlapIntervals.Count; index++)
            {
                var (start, end) = overlapIntervals[index];
                var currentBeat = start;
                // 在区间开始前，初始化“最近结束值”为区间开始拍之前最近结束的事件值（可能来自区间之外）
                var prevTo = toEventsCopy.FindLast(e => e.EndBeat <= start);
                var prevForm = formEventsCopy.FindLast(e => e.EndBeat <= start);
                formOverlapEventLastEndValue = prevForm != null ? prevForm.EndValue : default;
                toOverlapEventLastEndValue = prevTo != null ? prevTo.EndValue : default;
                while (currentBeat < end)
                {
                    var nextBeat = currentBeat + cutLength;
                    // 以currentBeat为开始拍，nextBeat为结束拍，寻找cutedToEvents和cutedFormEvents内的事件
                    var toEvent =
                        cutedToEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
                    var formEvent =
                        cutedFormEvents.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);

                    // 计算合并后的值
                    var toStartValue = toEvent != null ? toEvent.StartValue : toOverlapEventLastEndValue;
                    var formStartValue = formEvent != null ? formEvent.StartValue : formOverlapEventLastEndValue;
                    var startValue = AddValues(toStartValue, formStartValue);

                    var toEndValue = toEvent != null ? toEvent.EndValue : toOverlapEventLastEndValue;
                    var formEndValue = formEvent != null ? formEvent.EndValue : formOverlapEventLastEndValue;
                    var endValue = AddValues(toEndValue, formEndValue);

                    var newEvent = new RePhiEdit.Event<T>
                    {
                        StartBeat = currentBeat,
                        EndBeat = nextBeat,
                        StartValue = startValue!,
                        EndValue = endValue!,
                    };

                    allCutedEvents.Add(newEvent);

                    // 更新最后的结束值
                    if (toEvent != null) toOverlapEventLastEndValue = toEvent.EndValue;
                    if (formEvent != null) formOverlapEventLastEndValue = formEvent.EndValue;

                    currentBeat = nextBeat;
                }
            }

            // 把切割后的事件加入newEvents
            newEvents.AddRange(allCutedEvents);
            // 最后把newEvents赋值给toEvents
            toEventsCopy = newEvents;

            // 按开始拍排序
            toEventsCopy.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }
        else // 如果没有重合事件,直接合并
        {
            foreach (var formEvent in formEventsCopy)
            {
                var previousToEvent = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat);
                var toEventValue = previousToEvent != null ? previousToEvent.EndValue : default;
                var mergedEvent = new RePhiEdit.Event<T>
                {
                    StartBeat = formEvent.StartBeat,
                    EndBeat = formEvent.EndBeat,
                    StartValue = AddValues(formEvent.StartValue, toEventValue)!,
                    EndValue = AddValues(formEvent.EndValue, toEventValue)!,
                    BezierPoints = formEvent.BezierPoints,
                    Easing = formEvent.Easing,
                    EasingLeft = formEvent.EasingLeft,
                    EasingRight = formEvent.EasingRight,
                    IsBezier = formEvent.IsBezier,
                };
                toEventsCopy.Add(mergedEvent);
            }


            // 合并后按开始拍排序
            toEventsCopy.Sort((a, b) =>
            {
                if (a.StartBeat < b.StartBeat) return -1;
                if (a.StartBeat > b.StartBeat) return 1;
                return 0;
            });
        }

        return toEventsCopy;
    }

    private static List<RePhiEdit.Event<T>>? RemoveUnlessEvent<T>(List<RePhiEdit.Event<T>>? events)
    {
        // 在确保events不是null的情况下Copy一份，防止对原始列表篡改
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy != null && eventsCopy.Count == 1 &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default(T)) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue, default(T)))
        {
            eventsCopy.RemoveAt(0);
        }

        return eventsCopy;
    }

    private static List<RePhiEdit.EventLayer>? RemoveUnlessLayer(List<RePhiEdit.EventLayer>? layers)
    {
        if (layers == null || layers.Count <= 1) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        // index非1 layer头部的0值事件去除
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = RemoveUnlessEvent(layer.AlphaEvents);
            layer.MoveXEvents = RemoveUnlessEvent(layer.MoveXEvents);
            layer.MoveYEvents = RemoveUnlessEvent(layer.MoveYEvents);
            layer.RotateEvents = RemoveUnlessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    public static RePhiEdit.EventLayer LayerMerge(List<RePhiEdit.EventLayer> layers)
    {
        var loc = Localizer.Create();
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
        {
            //OnWarning.Invoke("LayerMerge: layers count less than or equal to 1, no need to merge.");
            OnWarning.Invoke("LayerMerge" + loc["util.rpe.warn.layer_insufficient_quantity"]);
            return layers.FirstOrDefault() ?? new RePhiEdit.EventLayer();
        }

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new RePhiEdit.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents != null && layer.AlphaEvents.Count > 0)
                mergedLayer.AlphaEvents = EventMerge(mergedLayer.AlphaEvents, layer.AlphaEvents);
            if (layer.MoveXEvents != null && layer.MoveXEvents.Count > 0)
                mergedLayer.MoveXEvents = EventMerge(mergedLayer.MoveXEvents, layer.MoveXEvents);
            if (layer.MoveYEvents != null && layer.MoveYEvents.Count > 0)
                mergedLayer.MoveYEvents = EventMerge(mergedLayer.MoveYEvents, layer.MoveYEvents);
            if (layer.RotateEvents != null && layer.RotateEvents.Count > 0)
                mergedLayer.RotateEvents = EventMerge(mergedLayer.RotateEvents, layer.RotateEvents);
            if (layer.SpeedEvents != null && layer.SpeedEvents.Count > 0)
                mergedLayer.SpeedEvents = EventMerge(mergedLayer.SpeedEvents, layer.SpeedEvents);
        }

        return mergedLayer;
    }
}