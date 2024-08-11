using Wist.Backend.AstToIrCompiler;
using Wist.Backend.Executing;

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public interface IAstCompiler
{
    public IExecutable Compile(IrImage root);
}