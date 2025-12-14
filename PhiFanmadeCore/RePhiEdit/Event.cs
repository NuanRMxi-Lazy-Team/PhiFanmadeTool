using System;
using System.Linq;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class Event<T>
        {
            /// <summary>
            /// 是否为贝塞尔曲线
            /// </summary>
            [JsonProperty("bezier")] [JsonConverter(typeof(BoolConverter))]
            public bool IsBezier = false; // 是否为贝塞尔曲线

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
            [JsonProperty("easingType")] public Easing Easing = new Easing(1); // 缓动类型

            /// <summary>
            /// 事件开始数值
            /// </summary>
            [JsonProperty("start")] public T StartValue; // 开始值

            /// <summary>
            /// 事件结束数值
            /// </summary>
            [JsonProperty("end")] public T EndValue; // 结束值

            /// <summary>
            /// 事件开始拍
            /// </summary>
            [JsonProperty("startTime")] public Beat StartBeat = new Beat(new[] { 0, 0, 1 }); // 开始时间

            /// <summary>
            /// 事件结束拍
            /// </summary>
            [JsonProperty("endTime")] public Beat EndBeat = new Beat(new[] { 1, 0, 1 }); // 结束时间

            /// <summary>
            /// 获取某个拍在这个事件中的值
            /// </summary>
            /// <param name="beat">指定拍</param>
            /// <returns>指定拍时，此事件的数值</returns>
            public T GetValueAtBeat(Beat beat)
            {
                var t = (beat - StartBeat) / (EndBeat - StartBeat);

                // 如果启用了贝塞尔曲线,使用 Bezier.Do
                if (IsBezier)
                {
                    // 检查 T 的类型并调用相应的方法
                    if (typeof(T) == typeof(float))
                        return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToSingle(StartValue),
                            Convert.ToSingle(EndValue), EasingLeft, EasingRight);
                    else if (typeof(T) == typeof(double))
                        return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToDouble(StartValue),
                            Convert.ToDouble(EndValue), EasingLeft, EasingRight);
                    else if (typeof(T) == typeof(int))
                        return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToInt32(StartValue),
                            Convert.ToInt32(EndValue), EasingLeft, EasingRight);
                    else if (typeof(T) == typeof(byte[]))
                    {
                        byte[] startBytes = StartValue as byte[];
                        byte[] endBytes = EndValue as byte[];
                        if (startBytes == null || endBytes == null)
                            throw new InvalidOperationException("Start or End is not a byte array, or is null.");
                        if (startBytes.Length != endBytes.Length)
                            throw new InvalidOperationException(
                                "Byte arrays must be of the same length for interpolation.");
                        byte[] result = new byte[startBytes.Length];
                        for (int i = 0; i < startBytes.Length; i++)
                            result[i] = Bezier.Do(BezierPoints, t, startBytes[i], endBytes[i], EasingLeft, EasingRight);
                        return (T)(object)result;
                    }
                    else
                        throw new NotSupportedException($"类型 {typeof(T)} 不受支持。");
                }

                // 检查 T 的类型并调用相应的方法
                if (typeof(T) == typeof(float))
                    return (T)(object)Easing.Do(EasingLeft, EasingRight, Convert.ToSingle(StartValue),
                        Convert.ToSingle(EndValue),
                        t);
                else if (typeof(T) == typeof(double))
                    return (T)(object)Easing.Do(EasingLeft, EasingRight, Convert.ToDouble(StartValue),
                        Convert.ToDouble(EndValue),
                        t);
                else if (typeof(T) == typeof(int))
                    return (T)(object)Easing.Do(EasingLeft, EasingRight, Convert.ToInt32(StartValue),
                        Convert.ToInt32(EndValue), t);
                else if (typeof(T) == typeof(byte[]))
                {
                    byte[] startBytes = StartValue as byte[];
                    byte[] endBytes = EndValue as byte[];
                    if (startBytes == null || endBytes == null)
                        throw new InvalidOperationException("Start or End is not a byte array, or is null.");
                    if (startBytes.Length != endBytes.Length)
                        throw new InvalidOperationException(
                            "Byte arrays must be of the same length for interpolation.");
                    byte[] result = new byte[startBytes.Length];
                    for (int i = 0; i < startBytes.Length; i++)
                        result[i] = Easing.Do(EasingLeft, EasingRight, startBytes[i], endBytes[i], t);
                    return (T)(object)result;
                }
                else
                    throw new NotSupportedException($"类型 {typeof(T)} 不受支持。");
            }

            public Event<T> Clone()
            {
                return new Event<T>
                {
                    IsBezier = IsBezier,
                    BezierPoints = BezierPoints.ToArray(),
                    EasingLeft = EasingLeft,
                    EasingRight = EasingRight,
                    Easing = Easing,
                    StartValue = StartValue,
                    EndValue = EndValue,
                    StartBeat = StartBeat,
                    EndBeat = EndBeat
                };
            }
        }
    }
}