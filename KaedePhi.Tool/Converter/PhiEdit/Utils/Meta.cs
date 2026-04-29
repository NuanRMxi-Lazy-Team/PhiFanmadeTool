namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public static class Meta
{
    private const int OffsetOffset = 175;

    public static Kpc.Meta ConvertMeta(Pe.Chart src) => new()
    {
        Offset = src.Offset - OffsetOffset
    };

    public static int GetPeOffset(Kpc.Meta src) => src.Offset + OffsetOffset;
}
