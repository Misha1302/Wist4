using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;

namespace Wist.Backend.Compiler.AsmGenerators;

public interface IAstCompiler
{
    public IExecutable Compile(AstNode root);
}