﻿namespace Wist.Lexer;

using Wist.Lexer.Lexemes;

public static class LexerExtensions
{
    public static LexemeDeclaration Get(this List<LexemeDeclaration> lds, LexemeType lexemeType)
    {
        return lds.First(x => x.LexemeType == lexemeType);
    }
}