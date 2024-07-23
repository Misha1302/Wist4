namespace Wist.Backend.Compiler;

using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;

public interface IAstCompiler
{
    public IExecutable Compile(AstNode root);
}