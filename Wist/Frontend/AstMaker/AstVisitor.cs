namespace Wist.Frontend.AstMaker;

public class AstVisitor
{
    public void Visit(AstNode root, Action<AstNode> handler)
    {
        foreach (var child in root.Children)
            Visit(child, handler);

        handler(root);
    }
}