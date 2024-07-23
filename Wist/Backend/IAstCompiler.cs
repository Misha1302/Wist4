namespace Wist.Backend;

using Wist.Frontend.AstMaker;

public interface IAstCompiler
{
    public IExecutable Compile(AstNode root);
}