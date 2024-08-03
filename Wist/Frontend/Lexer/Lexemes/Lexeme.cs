namespace Wist.Frontend.Lexer.Lexemes;

public record Lexeme(LexemeType LexemeType, string Text)
{
    public override string ToString()
    {
        return $"{Text}:{LexemeType}";
    }
}