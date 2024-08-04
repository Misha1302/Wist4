using Iced.Intel;
using Wist.Logger;

namespace Wist.Backend.Executing;

public abstract class AsmExecutableBase(Assembler asm, ILogger logger) : IExecutable
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

    public abstract unsafe delegate*<T> MakeFunction<T>();
}