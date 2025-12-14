using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.RePhiEdit
{
    public static partial class RePhiEdit
    {
        /// <summary>
        /// RePhiEdit的标准节拍，表示为 beat[0]:beat[1]/beat[2]
        /// 使用float或double隐式转换时，返回 CurBeat = beat[1] / beat[2] + beat[0]
        /// 使用int[]隐式转换时，返回原始数组
        /// </summary>
        [JsonConverter(typeof(BeatJsonConverter))]
        public class Beat : IComparable<Beat>
        {
            private readonly int[] _beat;

            public Beat(int[] beatArray)
            {
                _beat = beatArray ?? new[] { 0, 0, 1 };
            }

            public Beat(double beat)
            {
                // 将 double 转换为 Beat 结构，使用连分数算法获得最佳分数近似
                int wholePart = (int)Math.Floor(beat);
                double fractionalPart = beat - wholePart;

                if (Math.Abs(fractionalPart) < 1e-9)
                {
                    _beat = new[] { wholePart, 0, 1 };
                    return;
                }

                // 使用连分数算法找到最佳分数表示（限制分母最大为1000）
                int numerator = 1, denominator = 0;
                int prevNumerator = 0, prevDenominator = 1;
                double remaining = fractionalPart;
                const int maxDenominator = 1000;

                for (int iteration = 0; iteration < 20 && denominator <= maxDenominator; iteration++)
                {
                    int digit = (int)Math.Floor(remaining);

                    int tempNum = digit * numerator + prevNumerator;
                    int tempDen = digit * denominator + prevDenominator;

                    if (tempDen > maxDenominator) break;

                    prevNumerator = numerator;
                    prevDenominator = denominator;
                    numerator = tempNum;
                    denominator = tempDen;

                    remaining = remaining - digit;
                    if (Math.Abs(remaining) < 1e-9 || Math.Abs((double)numerator / denominator - fractionalPart) < 1e-9)
                        break;

                    remaining = 1.0 / remaining;
                }

                // 如果连分数算法没有找到好的近似，使用简单的四舍五入方法
                if (denominator == 0 || denominator > maxDenominator)
                {
                    denominator = 1000;
                    numerator = (int)Math.Round(fractionalPart * denominator);
                    int gcd = GCD(numerator, denominator);
                    numerator /= gcd;
                    denominator /= gcd;
                }

                _beat = new[] { wholePart, numerator, denominator };
            }

            /// <summary>
            /// 获得最大公约数
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns>最大公约数</returns>
            private static int GCD(int a, int b)
            {
                while (b != 0)
                {
                    int temp = b;
                    b = a % b;
                    a = temp;
                }

                return a;
            }

            private static long GCD(long a, long b)
            {
                while (b != 0)
                {
                    long temp = b;
                    b = a % b;
                    a = temp;
                }

                return a;
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
            // 定义两个Beat对象的加法运算符
            public static Beat operator +(Beat a, Beat b)
            {
                // 基于Beat = beat[1] / beat[2] + beat[0]的定义进行加法运算
                var wholePart = a[0] + b[0];

                // 使用 long 防止中间计算溢出
                long numerator = (long)a[1] * b[2] + (long)b[1] * a[2];
                long denominator = (long)a[2] * b[2];

                // 处理进位
                if (numerator >= denominator)
                {
                    long carry = numerator / denominator;
                    wholePart += (int)carry;
                    numerator %= denominator;
                }

                // 处理负数情况
                if (numerator < 0)
                {
                    long borrowCount = (-numerator + denominator - 1) / denominator;
                    wholePart -= (int)borrowCount;
                    numerator += borrowCount * denominator;
                }

                // 如果分子为0,直接返回简化的Beat
                if (numerator == 0)
                {
                    return new Beat(new[] { wholePart, 0, 1 });
                }

                // 约分
                long gcd = GCD(Math.Abs(numerator), denominator);
                numerator /= gcd;
                denominator /= gcd;

                // 检查是否超出 int 范围
                if (numerator > int.MaxValue || denominator > int.MaxValue)
                {
                    throw new OverflowException(
                        "Beat calculation resulted in values too large for int representation.");
                }

                return new Beat(new[] { wholePart, (int)numerator, (int)denominator });
            }


            // 定义两个Beat对象的减法运算符
            public static Beat operator -(Beat a, Beat b)
            {
                // 基于Beat = beat[1] / beat[2] + beat[0]的定义进行减法运算
                var wholePart = a[0] - b[0];

                // 使用 long 防止中间计算溢出
                long numerator = (long)a[1] * b[2] - (long)b[1] * a[2];
                long denominator = (long)a[2] * b[2];

                // 处理负数情况
                if (numerator < 0)
                {
                    long borrowCount = (-numerator + denominator - 1) / denominator;
                    wholePart -= (int)borrowCount;
                    numerator += borrowCount * denominator;
                }

                // 如果分子为0,直接返回简化的Beat
                if (numerator == 0)
                {
                    return new Beat(new[] { wholePart, 0, 1 });
                }

                // 约分
                long gcd = GCD(Math.Abs(numerator), denominator);
                numerator /= gcd;
                denominator /= gcd;

                // 检查是否超出 int 范围
                if (numerator > int.MaxValue || denominator > int.MaxValue)
                {
                    throw new OverflowException(
                        "Beat calculation resulted in values too large for int representation.");
                }

                return new Beat(new[] { wholePart, (int)numerator, (int)denominator });
            }

            // 定义两个Beat对象的比较运算符，强行使用double作为比较依据，float有精度问题
            public static bool operator <(Beat a, Beat b) => (double)a < (double)b;
            public static bool operator >(Beat a, Beat b) => (double)a > (double)b;
            public static bool operator <=(Beat a, Beat b) => (double)a <= (double)b;
            public static bool operator >=(Beat a, Beat b) => (double)a >= (double)b;

            public static bool operator ==(Beat a, Beat b)
            {
                if (ReferenceEquals(a, b)) return true;
                if (a is null || b is null) return false;
                return (double)a == (double)b;
            }

            public static bool operator !=(Beat a, Beat b) => !(a == b);

            /// <summary>
            /// 返回 Beat 的字符串表示，格式为 beat[0]:beat[1]/beat[2]
            /// </summary>
            /// <returns>beat[0]:beat[1]/beat[2]</returns>
            public override string ToString()
            {
                return $"{this[0]}:{this[1]}/{this[2]}";
            }

            /// <summary>
            /// 比较当前 Beat 与另一个 Beat 的大小
            /// </summary>
            /// <param name="other">要比较的另一个 Beat 对象</param>
            /// <returns>如果当前 Beat 小于 other 返回负数，等于返回 0，大于返回正数</returns>
            public int CompareTo(Beat other)
            {
                if (other == null) return 1;

                // 将两个 Beat 转换为 double 进行比较
                double thisValue = this;
                double otherValue = other;

                return thisValue.CompareTo(otherValue);
            }
        }
    }
}