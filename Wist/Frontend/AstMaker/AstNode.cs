using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.AstMaker;

public record AstNode(Lexeme Lexeme, List<AstNode> Children, AstNode? Parent)
{
    public AstNode? Parent = Parent;

    public override string ToString()
    {
        return this.ToStringExt();
    }
}