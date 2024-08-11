using Wist.Frontend.AstMaker;

namespace Wist.Backend.AstToIrCompiler;

public interface IAstToIrCompiler
{
    public IrImage Compile(AstNode root);
}