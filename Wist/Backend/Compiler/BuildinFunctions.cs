namespace Wist.Backend.Compiler;

public static class BuildinFunctions
{
    public static readonly unsafe ulong CallocPtr = (ulong)(delegate*<int, long>)&SimpleAllocator.Calloc;
    public static readonly unsafe ulong FreePtr = (ulong)(delegate*<long, void>)&SimpleAllocator.Free;
    public static readonly unsafe ulong WriteI64Ptr = (ulong)(delegate*<long, void>)&Console.WriteLine;
    public static readonly unsafe ulong WriteI64NoLnPtr = (ulong)(delegate*<long, void>)&WriteI64NoLn;
    public static readonly unsafe ulong WriteLnPtr = (ulong)(delegate*<void>)&Console.WriteLine;
    public static readonly unsafe ulong ReadI64Ptr = (ulong)(delegate*<long>)&ReadI64;

    private static long ReadI64()
    {
        return long.Parse(Console.ReadLine()!);
    }

    private static void WriteI64NoLn(long value)
    {
        Console.Write($"{value} ");
    }
}