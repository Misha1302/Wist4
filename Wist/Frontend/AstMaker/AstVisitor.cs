namespace Wist.Frontend.AstMaker;

public class AstVisitor
{
    public void Visit(AstNode root, Action<AstNode> handler, Predicate<AstNode> needToCompileChildren)
    {
        if (needToCompileChildren(root))
            foreach (var child in root.Children)
                Visit(child, handler, needToCompileChildren);

        handler(root);
    }
}