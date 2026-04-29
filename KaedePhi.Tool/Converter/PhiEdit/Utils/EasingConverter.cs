namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class EasingConverter
{
    /// <summary>
    /// KPC 缓动在 PE 中无对应项时抛出，用于触发切段拟合。
    /// </summary>
    public sealed class EasingNotSupportedException(int nrcEasing)
        : Exception($"NRC easing {nrcEasing} is unsupported in pe and requires linear slicing");

    public static int MapToKpc(int pe) => pe switch
    {
        1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
        8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
        14 => 15, 15 => 14, 16 => 18, 17 => 17, 18 => 21, 19 => 20,
        20 => 24, 21 => 23, 22 => 22, 23 => 25, 24 => 27, 25 => 26,
        26 => 30, 27 => 29, 28 => 31, 29 => 28, _ => 1
    };

    public static int MapToPe(int kpcEasing)
    {
        var mapped = kpcEasing switch
        {
            1 => 1,
            2 => 3,
            3 => 2,
            4 => 6,
            5 => 5,
            6 => 4,
            7 => 7,
            8 => 9,
            9 => 8,
            10 => 12,
            11 => 11,
            12 => 10,
            13 => 13,
            14 => 15,
            15 => 14,
            16 => throw new EasingNotSupportedException(16),
            17 => 17,
            18 => 16,
            19 => throw new EasingNotSupportedException(19),
            20 => 19,
            21 => 18,
            22 => 22,
            23 => 21,
            24 => 20,
            25 => 23,
            26 => 25,
            27 => 24,
            28 => 29,
            29 => 27,
            30 => 26,
            31 => 28,
            _ => 1
        };
        return mapped;
    }

    public static Kpc.Easing ConvertEasing(Pe.Easing src) => new(MapToKpc((int)src));
    public static Pe.Easing ConvertEasing(Kpc.Easing src, bool isBezier = false)
        => isBezier ? throw new EasingNotSupportedException(-1) : new Pe.Easing(MapToPe((int)src));
}
