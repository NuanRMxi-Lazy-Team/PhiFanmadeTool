namespace PhiFanmade.Tool.PhiFanmadeNrc.Converters.Utils;

public class NrcToCmdysjEasings
{
    internal static int MapEasingNumber(int nrcEasing)
    {
        var mapped = nrcEasing switch
        {
            1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
            8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
            14 => 15, 15 => 14,
            16 => throw new EasingNotSupportedException(16),
            17 => 17, 18 => 16,
            19 => throw new EasingNotSupportedException(19),
            20 => 19, 21 => 18, 22 => 22, 23 => 21, 24 => 20, 25 => 23,
            26 => 25, 27 => 24, 28 => 29, 29 => 27, 30 => 26, 31 => 28,
            _ => 1
        };
        return mapped;
    }
    
    /// <summary>
    /// NRC 缓动在 PE 中无对应项时抛出，用于触发切段拟合。
    /// </summary>
    public sealed class EasingNotSupportedException(int nrcEasing)
        : Exception($"NRC easing {nrcEasing} is unsupported in cmdysj and requires linear slicing");
}