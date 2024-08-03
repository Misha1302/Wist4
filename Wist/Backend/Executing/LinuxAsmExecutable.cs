using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Iced.Intel;
using Wist.Logger;

namespace Wist.Backend.Executing;

public class LinuxAsmExecutable(Assembler asm, ILogger logger) : IExecutable
{
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

    private unsafe delegate*<T> MakeFunction<T>()
    {
        var bin = ToBinary();
        var f = MemoryMappedFile.CreateNew(null, bin.Length, MemoryMappedFileAccess.ReadWriteExecute,
            MemoryMappedFileOptions.None, HandleInheritability.None);
        var stream = f.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWriteExecute);
        var ptr = stream.SafeMemoryMappedViewHandle.DangerousGetHandle();
        Marshal.Copy(bin, 0, ptr, bin.Length);

        return (delegate*<T>)ptr;
    }
}