namespace Wist.Frontend.AstMaker;

public class AstVisitor
{
    public void Visit(AstNode root, Action<AstNode> handler, Predicate<AstNode> needToCompileChildren,
        bool reverse = false)
    {
        // for used instead of foreach 'cause children may have changes

        if (needToCompileChildren(root))
            if (!reverse)
                // ReSharper disable once ForCanBeConvertedToForeach 
                for (var index = 0; index < root.Children.Count; index++)
                    Visit(root.Children[index], handler, needToCompileChildren);
            else
                for (var i = root.Children.Count - 1; i >= 0; i--)
                    Visit(root.Children[i], handler, needToCompileChildren);

        handler(root);
    }
}