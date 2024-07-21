namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public record AstNode(Lexeme Lexeme, List<AstNode> Children)
{
    public override string ToString() => $"'{Lexeme}' -> ({string.Join(", ", Children)})";
}