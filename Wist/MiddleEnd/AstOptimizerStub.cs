using Wist.Frontend.AstMaker;

namespace Wist.MiddleEnd;

public class AstOptimizerStub : IAstOptimizer
{
    public AstNode OptimizeAst(AstNode root)
    {
        return root;
    }
}