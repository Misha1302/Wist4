namespace Wist.Frontend.AstMaker;

public class AstVisitor
{
    public void Visit(AstNode root, Action<AstNode> handler, Predicate<AstNode> needToCompileChildren,
        bool reverse = false)
    {
        if (needToCompileChildren(root))
            if (!reverse)
                foreach (var child in root.Children)
                    Visit(child, handler, needToCompileChildren);
            else
                for (var i = root.Children.Count - 1; i >= 0; i--)
                    Visit(root.Children[i], handler, needToCompileChildren);

        handler(root);
    }
}