namespace Wist.Frontend.AstMaker;

public static class AstNodeExtensions
{
    public static string ToStringExt(this AstNode astNode)
    {
        var depth = astNode.GetScopeDepth();
        if (astNode.Children.Count != 0)
        {
            var spacesBefore = new string(' ', depth * 2);
            return
                $"[{astNode.Parent?.Lexeme.Text} {depth}] {astNode.Lexeme} -> \n{spacesBefore}({string.Join($", \n{spacesBefore}", astNode.Children)})";
        }

        return $"[{astNode.Parent?.Lexeme.Text} {depth}] {astNode.Lexeme}";
    }

    public static int GetScopeDepth(this AstNode astNode)
    {
        if (astNode.Parent == null) return 0;

        var parent = astNode.Parent;
        var depth = 0;
        do
        {
            depth++;
            parent = parent.Parent;
        } while (parent != null);

        return depth;
    }
}