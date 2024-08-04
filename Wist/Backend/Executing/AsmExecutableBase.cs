using Iced.Intel;
using Wist.Backend.Compiler;
using Wist.Logger;

namespace Wist.Backend.Executing;

public abstract class AsmExecutableBase(Assembler asm, IDebugData debugData, ILogger logger) : IExecutable
{
    public unsafe long Execute()
    {
        logger.Log(AsmPrinter.PrintCodeToString(asm, debugData));
        var functionPointer = MakeFunction<long>(out var bin);

        logger.Log($"Successfully compiled assembly code. " +
                   $"Address: 0x{(ulong)functionPointer:x8}. " +
                   $"Size in bytes: {bin.Length}");

        GC.Collect(0, GCCollectionMode.Forced, true);
        GC.Collect(1, GCCollectionMode.Forced, true);
        GC.Collect(2, GCCollectionMode.Forced, true);
        var exitCode = functionPointer();

        logger.Log($"Program {(exitCode == 0 ? "successfully finished" : "failed")} with exit code {exitCode}");

        return exitCode;
    }

    public byte[] ToBinary()
    {
        var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        return stream.ToArray();
    }

    public abstract unsafe delegate*<T> MakeFunction<T>(out byte[] bin);
}