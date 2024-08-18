using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.AstMaker;

public sealed class AstNode
{
    public readonly List<AstNode> Children;
    public readonly int Hash;
    public readonly Lexeme Lexeme;
    public readonly int Number;
    public AstNode? Parent;

    public AstNode(Lexeme lexeme, List<AstNode> children, AstNode? parent, int number)
    {
        Number = number;
        Children = children;
        Lexeme = lexeme;
        Parent = parent;
        Hash = GetHashCode();
    }

    // public override int GetHashCode()
    // {
    //     // ReSharper disable NonReadonlyMemberInGetHashCode
    //     return Children.Aggregate(
    //         Lexeme.GetHashCode(),
    //         (a, b) => a ^ b.GetHashCode()
    //     );
    // }

    public override string ToString()
    {
        return this.ToStringExt();
    }
}