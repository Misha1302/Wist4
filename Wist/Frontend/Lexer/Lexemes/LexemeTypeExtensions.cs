namespace Wist.Frontend.Lexer.Lexemes;

public static class LexemeTypeExtensions
{
    public static bool IsOperation(this LexemeType lexemeType)
    {
        return lexemeType is LexemeType.Plus or LexemeType.Minus or LexemeType.Mul or LexemeType.Div
            or LexemeType.Modulo;
    }

    public static bool IsConst(this LexemeType lexemeType)
    {
        return lexemeType is LexemeType.Int64 or LexemeType.Float64;
    }
}