using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.AstMaker;

public class AstNode(Lexeme lexeme, List<AstNode> children, AstNode? parent, int number)
{
    public readonly int Number = number;
    public List<AstNode> Children = children;
    public Lexeme Lexeme = lexeme;
    public AstNode? Parent = parent;

    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return Children.Aggregate(
            Lexeme.GetHashCode(),
            (a, b) => a ^ b.GetHashCode()
        );
    }

    public override string ToString()
    {
        return this.ToStringExt();
    }
}