namespace Wist.Backend.Compiler;

using System.Runtime.CompilerServices;

public static class SimpleAllocator
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly List<byte[]> _arrays = [];

    public static unsafe long Calloc(int bytes)
    {
        var arr = GC.AllocateArray<byte>(bytes, true);
        _arrays.Add(arr);

        fixed (byte* ptr = arr)
            return (long)ptr;
    }

    public static unsafe void Free(long ptr)
    {
        _arrays.Remove(Unsafe.AsRef<byte[]>((void*)ptr));
    }
}