namespace Wist.Backend;

using Iced.Intel;
using Wist.Frontend.AstMaker;
using Wist.Logger;
using AsmExecutable = Wist.Backend.Executing.AsmExecutable;

public class AstCompilerToAsm(ILogger logger) : IAstCompiler
{
    public IExecutable Compile(AstNode root)
    {
        var assembler = new Assembler(64);
        assembler.mov(rax, 123);
        assembler.ret();
        return new AsmExecutable(assembler, logger);
    }
}