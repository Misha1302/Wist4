using System.Globalization;

namespace Wist.Backend.IrToAsmCompiler;

public static class ValueExtensions
{
    public static unsafe TTo As<TFrom, TTo>(this TFrom value) where TFrom : unmanaged where TTo : unmanaged
    {
        return *(TTo*)&value;
    }

    public static long ToLong(this string value)
    {
        return long.Parse(value.Replace("_", ""));
    }

    public static double ToDouble(this string value)
    {
        return double.Parse(value.Replace("_", ""), CultureInfo.InvariantCulture);
    }
}