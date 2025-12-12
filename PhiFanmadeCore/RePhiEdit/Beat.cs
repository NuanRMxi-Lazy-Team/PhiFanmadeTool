using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    /// <summary>
    /// RePhiEdit的标准节拍，表示为 beat[0]:beat[1]/beat[2]
    /// 使用float或double隐式转换时，返回 CurBeat = beat[1] / beat[2] + beat[0]
    /// 使用int[]隐式转换时，返回原始数组
    /// </summary>
    [JsonConverter(typeof(BeatJsonConverter))]
    public class Beat 
    {
        private readonly int[] _beat;

        public Beat(int[] beatArray)
        {
            _beat = beatArray ?? new[]{0, 0, 1};
        }

        public int this[int index]
        {
            get
            {
                if (index > 2)
                    throw new IndexOutOfRangeException("RePhiEdit Beat count can not be greater than 3.");
                return _beat[index];
            }
            set
            {
                if (index > 2)
                    throw new IndexOutOfRangeException("RePhiEdit Beat count can not be greater than 3.");
                _beat[index] = value;
            }
        }

        // 隐式转换为 float，返回 CurBeat
        public static implicit operator float(Beat beat) => (float)beat[1] / beat[2] + beat[0];

        // 隐式转换为 double，返回 CurBeat
        public static implicit operator double(Beat beat) => (double)beat[1] / beat[2] + beat[0];

        // 隐式转换为 int[]，返回 _beat
        public static implicit operator int[](Beat beat) => beat._beat;

        // 定义两个Beat对象的加法运算符
        public static Beat operator +(Beat a, Beat b)
        {
            // 基于Beat = beat[1] / beat[2] + beat[0]的定义进行加法运算
            var wholePart = a[0] + b[0];
            var numerator = a[1] * b[2] + b[1] * a[2];
            var denominator = a[2] * b[2];
            // 处理负数情况
            if (numerator < 0)
            {
                int borrowCount = (-numerator + denominator - 1) / denominator;
                wholePart -= borrowCount;
                numerator += borrowCount * denominator;
            }

            return new Beat(new[]{wholePart, numerator, denominator});
        }

        // 定义两个Beat对象的减法运算符
        public static Beat operator -(Beat a, Beat b)
        {
            // 基于Beat = beat[1] / beat[2] + beat[0]的定义进行减法运算
            var wholePart = a[0] - b[0];
            var numerator = a[1] * b[2] - b[1] * a[2];
            var denominator = a[2] * b[2];
            // 处理负数情况
            if (numerator < 0)
            {
                int borrowCount = (-numerator + denominator - 1) / denominator;
                wholePart -= borrowCount;
                numerator += borrowCount * denominator;
            }

            return new Beat(new[]{wholePart, numerator, denominator});
        }

        /// <summary>
        /// 返回 Beat 的字符串表示，格式为 beat[0]:beat[1]/beat[2]
        /// </summary>
        /// <returns>beat[0]:beat[1]/beat[2]</returns>
        public override string ToString()
        {
            return $"{this[0]}:{this[1]}/{this[2]}";
        }
    }

    public class BeatJsonConverter : JsonConverter<Beat>
    {
        public override void WriteJson(JsonWriter writer, Beat value, JsonSerializer serializer)
        {
            // 将 Beat 序列化为 int[] 数组
            int[] beatArray = value;
            serializer.Serialize(writer, beatArray);
        }

        public override Beat ReadJson(JsonReader reader, Type objectType, Beat existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            // 从 int[] 数组反序列化为 Beat
            var beatArray = serializer.Deserialize<int[]>(reader);
            return new Beat(beatArray);
        }
    }
}