using Iced.Intel;
using Wist.Backend.IrToAsmCompiler.DebugData;
using Wist.Statistics.Logger;

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

        // IDK why, but if gc collection our garbage, program works correctly. Otherwise, exit code usually is 139
        GC.Collect();
        var exitCode = functionPointer();
        // but if gc collect garbage here, exit code usually is 139 again
        // GC.Collect();

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