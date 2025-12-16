using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
// STJ 特性使用完全限定名

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        public class Event<T>
        {
            /// <summary>
            /// 是否为贝塞尔曲线
            /// </summary>
            [JsonProperty("bezier")] [Newtonsoft.Json.JsonConverter(typeof(BoolConverter))]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("bezier"), System.Text.Json.Serialization.JsonConverter(typeof(StjBoolConverter))]
#endif
            public bool IsBezier = false; // 是否为贝塞尔曲线

            /// <summary>
            /// 贝塞尔曲线控制点
            /// </summary>
            [JsonProperty("bezierPoints")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("bezierPoints")]
#endif
            public float[] BezierPoints = new float[4]; // 贝塞尔曲线点

            /// <summary>
            /// 缓动截取左界限
            /// </summary>
            [JsonProperty("easingLeft")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("easingLeft")]
#endif
            public float EasingLeft = 0.0f; // 缓动开始

            /// <summary>
            /// 缓动截取右界限
            /// </summary>
            [JsonProperty("easingRight")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("easingRight")]
#endif
            public float EasingRight = 1.0f; // 缓动结束

            /// <summary>
            /// 缓动类型
            /// </summary>
            [JsonProperty("easingType")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("easingType"), System.Text.Json.Serialization.JsonConverter(typeof(StjEasingJsonConverter))]
#endif
            public Easing Easing = new Easing(1); // 缓动类型

            /// <summary>
            /// 事件开始数值
            /// </summary>
            [JsonProperty("start")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("start")]
#endif
            public T StartValue; // 开始值

            /// <summary>
            /// 事件结束数值
            /// </summary>
            [JsonProperty("end")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("end")]
#endif
            public T EndValue; // 结束值

            /// <summary>
            /// 事件开始拍
            /// </summary>
            [JsonProperty("startTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("startTime"), System.Text.Json.Serialization.JsonConverter(typeof(StjBeatJsonConverter))]
#endif
            public Beat StartBeat = new Beat(new[] { 0, 0, 1 }); // 开始时间

            /// <summary>
            /// 事件结束拍
            /// </summary>
            [JsonProperty("endTime")]
#if !NETSTANDARD2_1
            [System.Text.Json.Serialization.JsonPropertyName("endTime"), System.Text.Json.Serialization.JsonConverter(typeof(StjBeatJsonConverter))]
#endif
            public Beat EndBeat = new Beat(new[] { 1, 0, 1 }); // 结束时间

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

            private bool IsImmutableType(Type type)
            {
                // 值类型是不可变的
                if (type.IsValueType)
                    return true;

                // 字符串是不可变的
                if (type == typeof(string))
                    return true;

                // 检查常见的不可变类型
                if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                    type == typeof(TimeSpan) || type == typeof(Guid) ||
                    type == typeof(Uri) || type == typeof(Version))
                    return true;

                // 检查是否为枚举
                if (type.IsEnum)
                    return true;

                return false;
            }

            private TValue DeepClone<TValue>(TValue value)
            {
                if (value == null)
                    return default(TValue);

                var type = typeof(TValue);

                // 不可变类型直接返回
                if (IsImmutableType(type))
                    return value;

                // 处理数组
                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    var array = value as Array;
                    var clonedArray = Array.CreateInstance(elementType, array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var element = array.GetValue(i);
                        if (IsImmutableType(elementType))
                            clonedArray.SetValue(element, i);
                        else
                        {
                            var cloneMethod = typeof(Event<T>).GetMethod("DeepClone", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(elementType);
                            clonedArray.SetValue(cloneMethod.Invoke(this, new[] { element }), i);
                        }
                    }
                    return (TValue)(object)clonedArray;
                }

                // 处理泛型集合 (List<T>, Dictionary<K,V>, etc.)
                if (type.IsGenericType)
                {
                    var genericTypeDef = type.GetGenericTypeDefinition();

                    // List<T>
                    if (genericTypeDef == typeof(List<>))
                    {
                        var elementType = type.GetGenericArguments()[0];
                        var list = value as System.Collections.IList;
                        var clonedListType = typeof(List<>).MakeGenericType(elementType);
                        var clonedList = Activator.CreateInstance(clonedListType) as System.Collections.IList;

                        foreach (var item in list)
                        {
                            if (IsImmutableType(elementType))
                                clonedList.Add(item);
                            else
                            {
                                var cloneMethod = typeof(Event<T>).GetMethod("DeepClone", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .MakeGenericMethod(elementType);
                                clonedList.Add(cloneMethod.Invoke(this, new[] { item }));
                            }
                        }
                        return (TValue)clonedList;
                    }
                }

                // 尝试调用对象的 Clone() 方法
                var cloneMethodInfo = type.GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (cloneMethodInfo != null && cloneMethodInfo.ReturnType == type)
                {
                    return (TValue)cloneMethodInfo.Invoke(value, null);
                }

                // 使用反射进行浅拷贝并递归处理引用类型字段
                var cloned = Activator.CreateInstance(type);
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(value);
                    if (fieldValue == null)
                        continue;

                    var fieldType = field.FieldType;
                    if (IsImmutableType(fieldType))
                        field.SetValue(cloned, fieldValue);
                    else
                    {
                        var cloneMethod = typeof(Event<T>).GetMethod("DeepClone", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(fieldType);
                        field.SetValue(cloned, cloneMethod.Invoke(this, new[] { fieldValue }));
                    }
                }

                return (TValue)cloned;
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
                    StartValue = DeepClone(StartValue),
                    EndValue = DeepClone(EndValue),
                    StartBeat = new Beat((int[])StartBeat),
                    EndBeat = new Beat((int[])EndBeat)
                };
            }
        }
    }
}