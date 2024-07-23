namespace Wist.MiddleEnd;

using Wist.Frontend.AstMaker;

public interface IAstOptimizer
{
    public AstNode OptimizeAst(AstNode root);
}