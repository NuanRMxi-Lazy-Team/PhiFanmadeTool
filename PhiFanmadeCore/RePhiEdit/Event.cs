using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit;

public class Event
{
    [JsonProperty("bezier")] public int Bezier; // 是否为贝塞尔曲线
    [JsonProperty("bezierPoints")] public float[] BezierPoints = new float[4]; // 贝塞尔曲线点
    [JsonProperty("easingLeft")] public float EasingLeft; // 缓动开始
    [JsonProperty("easingRight")] public float EasingRight = 1.0f; // 缓动结束
    [JsonProperty("easingType")] public Easing EasingType = new(1); // 缓动类型
    [JsonProperty("start")] public float Start; // 开始值
    [JsonProperty("end")] public float End; // 结束值
    [JsonProperty("startTime")] public Beat StartBeat = new([0, 0, 1]); // 开始时间
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
        return EasingType.Do(EasingLeft,EasingRight,Start,End,t);
    }
}