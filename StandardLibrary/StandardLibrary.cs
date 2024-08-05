using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StandardLibrary;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
public static class StandardLibrary
{
    public static unsafe long Float64Add(long a, long b)
    {
        var c = a.As<long, double>() + b.As<long, double>();
        return *(long*)&c;
    }

    public static unsafe long Float64Sub(long a, long b)
    {
        var c = a.As<long, double>() - b.As<long, double>();
        return *(long*)&c;
    }

    public static unsafe long Float64Mul(long a, long b)
    {
        var c = a.As<long, double>() * b.As<long, double>();
        return *(long*)&c;
    }

    public static unsafe long Float64Div(long a, long b)
    {
        var c = a.As<long, double>() / b.As<long, double>();
        return *(long*)&c;
    }

    public static unsafe long ToFloat64(long charsArrayPtr)
    {
        var d = double.Parse(ArrToStr(charsArrayPtr));
        return *(long*)&d;
    }

    public static long I64ToStr(long value)
    {
        var s = value.ToString();
        var ptr = Calloc(s.Length * sizeof(char) + 8) + 8;
        WriteMemI64(ptr - 8, s.Length);
        for (var i = 0; i < s.Length; i++)
            WriteMemI64(ptr + i * sizeof(char), s[i]);
        return ptr;
    }

    private static unsafe string ArrToStr(long ptr)
    {
        var len = ReadMemI64(ptr - 8);
        var sb = new StringBuilder(16);
        for (var i = 0; i < len; i++)
            sb.Append(Unsafe.Read<char>((void*)(ptr + i * sizeof(char))));
        return sb.ToString();
    }


    public static unsafe void WriteF64(long value)
    {
        Console.WriteLine(*(double*)&value);
    }

    public static void WriteI64(long value)
    {
        Console.WriteLine(value);
    }

    public static unsafe long ReadMemI64(long address)
    {
        return Unsafe.Read<long>((void*)address);
    }

    public static unsafe void WriteMemI64(long address, long value)
    {
        Unsafe.Write((void*)address, value);
    }

    public static long Calloc(long bytes)
    {
        return Marshal.UnsafeAddrOfPinnedArrayElement(GC.AllocateArray<byte>((int)bytes, true), 0);
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