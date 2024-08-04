using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Logger;

namespace Wist.Backend.Executing;

public class LinuxAsmExecutable(Assembler asm, ILogger logger) : AsmExecutableBase(asm, logger)
{
    public override unsafe delegate*<T> MakeFunction<T>()
    {
        var bin = ToBinary();
        var memoryMappedFile = MemoryMappedFile.CreateNew(null, bin.Length, MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions.None, HandleInheritability.None);
        var stream = memoryMappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWriteExecute);
        var ptr = stream.SafeMemoryMappedViewHandle.DangerousGetHandle();
        Marshal.Copy(bin, 0, ptr, bin.Length);

        return (delegate*<T>)ptr;
    }
}