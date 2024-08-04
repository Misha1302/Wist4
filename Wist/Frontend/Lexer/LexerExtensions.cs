using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.Lexer;

public static class LexerExtensions
{
    public static LexemeDeclaration Get(this List<LexemeDeclaration> lds, LexemeType lexemeType)
    {
        return lds.First(x => x.LexemeType == lexemeType);
    }
}