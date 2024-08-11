using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Backend.IrToAsmCompiler.DebugData;
using Wist.Statistics.Logger;

namespace Wist.Backend.Executing;

public class LinuxAsmExecutable(Assembler asm, IDebugData debugData, ILogger logger)
    : AsmExecutableBase(asm, debugData, logger)
{
    public override unsafe delegate*<T> MakeFunction<T>(out byte[] bin)
    {
        bin = ToBinary();
        var memoryMappedFile = MemoryMappedFile.CreateNew(null, bin.Length, MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions.None, HandleInheritability.None);
        var stream = memoryMappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWriteExecute);
        var ptr = stream.SafeMemoryMappedViewHandle.DangerousGetHandle();
        Marshal.Copy(bin, 0, ptr, bin.Length);

#pragma warning disable CA1816
        GC.SuppressFinalize(memoryMappedFile);
        GC.SuppressFinalize(stream.SafeMemoryMappedViewHandle);
#pragma warning restore CA1816

        return (delegate*<T>)ptr;
    }
}