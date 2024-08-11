using System.Runtime.InteropServices;

namespace StandardLibrary;

public static class BasicAllocatorLib
{
    // to save length of array we need to alloc place for it
    private const int LengthItemSizeInBytes = 8;
    public static readonly string Prefix = "Allocator::";

    public static long Calloc(long bytes)
    {
        // IDK why, but this constant fixes bug with 134 error code - buffer overflowing
        const int strangeShadowSpace = 6;
        // AllocHGlobal - returns exact size array. AllocCoTaskMem - returns exact or greater size array
        return Marshal.AllocHGlobal((int)bytes + strangeShadowSpace + LengthItemSizeInBytes) + LengthItemSizeInBytes;
    }

    public static void Free(long ptr)
    {
        Marshal.FreeHGlobal((nint)ptr - LengthItemSizeInBytes);
    }
}