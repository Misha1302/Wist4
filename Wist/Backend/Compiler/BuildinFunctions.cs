namespace Wist.Backend.Compiler;

public static class BuildinFunctions
{
    public static readonly unsafe ulong CallocPtr = (ulong)(delegate*<int, long>)&Calloc;
    public static readonly unsafe ulong FreePtr = (ulong)(delegate*<long, void>)&Free;
    public static readonly unsafe ulong WriteI64Ptr = (ulong)(delegate*<long, void>)&Console.WriteLine;

    private static long Calloc(int bytes) => SimpleAllocator.Calloc(bytes);

    private static void Free(long ptr) => SimpleAllocator.Free(ptr);
}