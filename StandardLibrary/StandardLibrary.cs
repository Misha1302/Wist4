namespace StandardLibrary;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
public static class StandardLibrary
{
    public static void WriteI64(long value)
    {
        Console.WriteLine(value);
    }

    public static void WriteI64NoLn(long value)
    {
        Console.Write($"{value} ");
    }

    public static void WriteLn()
    {
        Console.WriteLine();
    }

    public static long ReadI64()
    {
        return Convert.ToInt64(Console.ReadLine());
    }

    public static void SetColor(long color)
    {
        Console.ForegroundColor = (ConsoleColor)color;
    }

    public static void ResetColor()
    {
        Console.ResetColor();
    }

    public static long GetRedColor()
    {
        return (long)ConsoleColor.Red;
    }

    public static long GetWhiteColor()
    {
        return (long)ConsoleColor.White;
    }

    public static long GetCyanColor()
    {
        return (long)ConsoleColor.Cyan;
    }
}