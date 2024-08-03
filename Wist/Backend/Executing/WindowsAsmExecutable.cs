using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Logger;

namespace Wist.Backend.Executing;

public partial class WindowsAsmExecutable(Assembler asm, ILogger logger) : IExecutable
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

    public byte[] ToBinary()
    {
        var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        return stream.ToArray();
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    private unsafe delegate*<T> MakeFunction<T>()
    {
        var bin = ToBinary();
        var ptr = VirtualAlloc(IntPtr.Zero, (uint)bin.Length, MemCommit, PageExecuteReadwrite);
        Marshal.Copy(bin, 0, ptr, bin.Length);

        return (delegate*<T>)ptr;
    }
}