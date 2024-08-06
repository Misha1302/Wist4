namespace StandardLibrary;

public static class StdConsoleLib
{
    public static readonly string Prefix = "Console:";

    public static void WriteNoLn(long strPtr)
    {
        Console.Write(StringOperationsLib.ArrToStr(strPtr));
    }

    public static void Write(long strPtr)
    {
        Console.WriteLine(StringOperationsLib.ArrToStr(strPtr));
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