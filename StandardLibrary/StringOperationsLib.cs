using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace StandardLibrary;

public static class StringOperationsLib
{
    public static readonly string Prefix = "Str:";

    private static readonly StringBuilder _sb = new(16);

    public static unsafe string ArrToStr(long ptr)
    {
        _sb.Clear();
        var len = MemoryOperationsLib.ReadMemI64(ptr - 8);
        for (var i = 0; i < len; i++)
            _sb.Append(Unsafe.Read<char>((void*)(ptr + i * sizeof(char))));
        return _sb.ToString();
    }


    public static long I64ToStr(long value)
    {
        return StringToCharArray(value.ToString());
    }

    public static long F64ToStr(double value)
    {
        return StringToCharArray(value.ToString(CultureInfo.CurrentCulture));
    }

    public static long StringToCharArray(string s)
    {
        var ptr = BasicAllocatorLib.Calloc(s.Length * sizeof(char));
        MemoryOperationsLib.WriteMemI64(ptr - 8, s.Length);
        for (var i = 0; i < s.Length; i++)
            MemoryOperationsLib.WriteMemI64(ptr + i * sizeof(char), s[i]);
        return ptr;
    }


    public static double StrToF64(long charsArrayPtr)
    {
        return double.Parse(ArrToStr(charsArrayPtr));
    }

    public static long StrToI64(long charsArrayPtr)
    {
        return long.Parse(ArrToStr(charsArrayPtr));
    }
}