namespace Wist.Backend.Executing;

using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Logger;

public partial class AsmExecutable(Assembler asm, ILogger logger) : IExecutable
{
    private const uint PageExecuteReadwrite = 0x40;
    private const uint MemCommit = 0x1000;

    public unsafe long Execute()
    {
        logger.Log(AsmPrinter.PrintCodeToString(asm));
        var functionPointer = MakeFunction<long>();
        logger.Log($"function created on address 0x{(long)functionPointer:x8}");
        return functionPointer();
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    private unsafe delegate*<T> MakeFunction<T>()
    {
        const ulong rip = 0x10;
        var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), rip);

        var ptr = VirtualAlloc(IntPtr.Zero, (uint)stream.Length, MemCommit, PageExecuteReadwrite);
        Marshal.Copy(stream.ToArray(), 0, ptr, (int)stream.Length);

        return (delegate*<T>)ptr;
    }
}