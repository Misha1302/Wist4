namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public record AstNode(Lexeme Lexeme, List<AstNode> Children, AstNode? Parent)
{
    public AstNode? Parent = Parent;


    public override string ToString()
    {
        var depth = GetScopeDepth();
        if (Children.Count != 0)
        {
            var spacesBefore = new string(' ', depth * 2);
            return
                $"[{Parent?.Lexeme.Text} {depth}] {Lexeme} -> \n{spacesBefore}({string.Join($", \n{spacesBefore}", Children)})";
        }

        return $"[{Parent?.Lexeme.Text} {depth}] {Lexeme}";
    }

    public int GetScopeDepth()
    {
        if (Parent == null) return 0;

        var parent = Parent;
        var depth = 0;
        do
        {
            depth++;
            parent = parent.Parent;
        } while (parent != null);

        return depth;
    }
}