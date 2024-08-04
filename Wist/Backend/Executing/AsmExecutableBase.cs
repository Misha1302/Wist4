using Iced.Intel;
using Wist.Logger;

namespace Wist.Backend.Executing;

public abstract class AsmExecutableBase(Assembler asm, ILogger logger) : IExecutable
{
    public unsafe long Execute()
    {
        logger.Log(AsmPrinter.PrintCodeToString(asm));
        var functionPointer = MakeFunction<long>(out var bin);

        logger.Log(
            $"Successfully compiled assembly code. Address: 0x{(ulong)functionPointer:x8}. Size in bytes: {bin.Length}");

        return functionPointer();
    }

    public byte[] ToBinary()
    {
        var stream = new MemoryStream();
        asm.Assemble(new StreamCodeWriter(stream), 0);
        return stream.ToArray();
    }

    public abstract unsafe delegate*<T> MakeFunction<T>(out byte[] bin);
}