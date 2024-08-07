namespace Wist.Frontend.Lexer.Lexemes;

public class Lexeme(LexemeType lexemeType, string text)
{
    public LexemeType LexemeType = lexemeType;
    public string Text = text;

    public override string ToString()
    {
        return $"{Text}:{LexemeType}";
    }
}