using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Backend.Compiler.DebugData;
using Wist.Logger;

namespace Wist.Backend.Executing;

public partial class WindowsAsmExecutable(Assembler asm, IDebugData debugData, ILogger logger)
    : AsmExecutableBase(asm, debugData, logger)
{
    private const uint PageExecuteReadwrite = 0x40;
    private const uint MemCommit = 0x1000;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    public override unsafe delegate*<T> MakeFunction<T>(out byte[] bin)
    {
        bin = ToBinary();
        var ptr = VirtualAlloc(IntPtr.Zero, (uint)bin.Length, MemCommit, PageExecuteReadwrite);
        Marshal.Copy(bin, 0, ptr, bin.Length);

        return (delegate*<T>)ptr;
    }
}