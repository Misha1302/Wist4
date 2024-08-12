namespace StandardLibrary;

public static class TypesLib
{
    public static readonly string Prefix = "Types::";

    public static double I64ToF64(long value)
    {
        return value;
    }

    public static long F64ToI64(double value)
    {
        return (long)(value + 0.0001);
    }
}