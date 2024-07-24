namespace StandardLibrary;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
public static class StandardLibrary
{
    public static void WriteI64(long value) => Console.WriteLine(value);
    public static void WriteI64NoLn(long value) => Console.Write($"{value} ");
    public static void WriteLn(long value) => Console.WriteLine();

    public static long ReadI64() => Convert.ToInt64(Console.ReadLine());

    public static void SetColor(long color) => Console.ForegroundColor = (ConsoleColor)color;
    public static void ResetColor(long color) => Console.ResetColor();

    public static long GetRedColor() => (long)ConsoleColor.Red;
    public static long GetWhiteColor() => (long)ConsoleColor.White;
    public static long GetCyanColor() => (long)ConsoleColor.Cyan;
}