using System.Runtime.CompilerServices;

namespace StandardLibrary;

public static class MemoryOperationsLib
{
    public static readonly string Prefix = "Mem::";

    public static unsafe long ReadMemI64(long address)
    {
        return Unsafe.Read<long>((void*)address);
    }

    public static unsafe long ReadMemI16(long address)
    {
        return Unsafe.Read<short>((void*)address);
    }

    public static unsafe void WriteMemI64(long address, long value)
    {
        Unsafe.Write((void*)address, value);
    }

    public static unsafe void WriteMemI16(long address, long value)
    {
        Unsafe.Write((void*)address, (short)value);
    }
}