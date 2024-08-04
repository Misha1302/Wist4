using Wist.Frontend.AstMaker;

namespace Wist.MiddleEnd;

public interface IAstOptimizer
{
    public AstNode OptimizeAst(AstNode root);
}