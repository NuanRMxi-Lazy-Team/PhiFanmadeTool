using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class Event
{
    /// <summary>
    /// 是否为贝塞尔曲线
    /// </summary>
    [JsonProperty("bezier")] [JsonConverter(typeof(BoolConverter))]
    public bool Bezier = false; // 是否为贝塞尔曲线
    /// <summary>
    /// 贝塞尔曲线控制点
    /// </summary>
    [JsonProperty("bezierPoints")] public float[] BezierPoints = new float[4]; // 贝塞尔曲线点
    /// <summary>
    /// 缓动截取左界限
    /// </summary>
    [JsonProperty("easingLeft")] public float EasingLeft = 0.0f; // 缓动开始
    /// <summary>
    /// 缓动截取右界限
    /// </summary>
    [JsonProperty("easingRight")] public float EasingRight = 1.0f; // 缓动结束
    /// <summary>
    /// 缓动类型
    /// </summary>
    [JsonProperty("easingType")] public Easing EasingType = new(1); // 缓动类型
    /// <summary>
    /// 事件开始数值
    /// </summary>
    [JsonProperty("start")] public float Start = 0f; // 开始值
    /// <summary>
    /// 事件结束数值
    /// </summary>
    [JsonProperty("end")] public float End = 0f; // 结束值
    /// <summary>
    /// 事件开始拍
    /// </summary>
    [JsonProperty("startTime")] public Beat StartBeat = new([0, 0, 1]); // 开始时间
    /// <summary>
    /// 事件结束拍
    /// </summary>
    [JsonProperty("endTime")] public Beat EndBeat = new([1, 0, 1]); // 结束时间

    /// <summary>
    /// 获取某个拍在这个事件中的值
    /// </summary>
    /// <param name="beat">指定拍</param>
    /// <returns>指定拍时，此事件的数值</returns>
    public float GetValueAtBeat(Beat beat)
    {
        //获得这个拍在这个事件的时间轴上的位置
        float t = (beat - StartBeat) / (EndBeat - StartBeat);
        return EasingType.Do(EasingLeft, EasingRight, Start, End, t);
    }
}