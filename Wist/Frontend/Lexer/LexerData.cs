﻿using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.Lexer;

using static LexemeType;
using Ld = LexemeDeclaration;

public static class LexerData
{
    public const string StructureAndFieldSeparator = ":";
    private static readonly ImprovedList<Ld> _lds;

    static LexerData()
    {
        _lds = new ImprovedList<Ld>
        {
            new(Comment, @"//[^\r\n]*"),
            new(NewLine, ";"),
            new(Arrow, "->"),
            new(LeftPar, "\\("),
            new(RightPar, "\\)"),
            new(LeftBrace, "\\{"),
            new(RightBrace, "\\}"),
            new(LeftRectangle, "\\["),
            new(RightRectangle, "\\]"),
            new(LessOrEquals, @"\<\="),
            new(GreaterOrEquals, @"\>\="),
            new(NotEqual, "!="),
            new(Negation, "!"),
            new(Equal, "=="),
            new(Character, "\'.\'"),
            new(Import, "import"),
            new(String, "\"[^\"]*?\""),
            new(As, "as"),
            new(Alias, "alias"),
            new(Is, "is"),
            new(Set, "="),
            new(Int64, @"-?\d+[\d_]*\d*"),
            new(Dot, "\\."),
            new(Modulo, "\\%"),
            new(WriteToMem, @"\<\-"),
            new(Plus, "\\+"),
            new(Mul, "\\*"),
            new(Div, "/"),
            new(If, "if"),
            new(Elif, "elif"),
            new(Else, "else"),
            new(Ret, "ret"),
            new(For, "for"),
            new(Spaces, "[ \t]+"),
            new(NewLine, "\r?\n"),
            new(LessThan, "\\<"),
            new(GreaterThan, "\\>"),
            new(Comma, ","),
            new(Minus, @"\-"),
            new(ReadMem, "@"),
            new(Identifier, "[a-zA-Z_][a-zA-Z_0-9:]*(?!(:))"),
        };

        const string whitespace = @"[ \t]";
        const string spacesWithNl = @"\s";

        var integer = _lds.Get(Int64).Pattern;
        var identifier = _lds.Get(Identifier).Pattern;
        var arrow = _lds.Get(Arrow).Pattern;
        var keywords = string.Join("|", _lds.Where(x => x.Pattern.All(char.IsLetter)).Select(x => x.Pattern));
        var first = $"(?<=[^a-zA-Z])(?!({keywords})){identifier}(?=({whitespace}+{identifier}))";
        var second = $"(?<=({arrow}{whitespace}*))(?!({keywords})){identifier}";
        _lds.Insert(0, new Ld(Type, $"({first})|({second})\\*?"));

        _lds.Insert(0, new Ld(Int32, $"{integer}s"));
        _lds.Insert(0, new Ld(Float64, integer + "\\." + integer));
        _lds.Insert(0, new Ld(GettingRef, $"&(?=({identifier}))"));
        _lds.Insert(0,
            new Ld(FunctionCall,
                $"(?<!(struct[ \\t]*|@)){identifier}(?=({_lds.Get(LeftPar).Pattern}))(?!(.+\\s*\\-\\>))"));
        _lds.Insert(0, new Ld(Goto, $"goto (?=({identifier}))"));
        _lds.Insert(0, new Ld(StructDeclaration, $"struct(?!({identifier}))"));
        _lds.Insert(0,
            new Ld(FunctionDeclaration,
                $@"(?=({whitespace}*))(?<!([a-zA-Z0-9:]+)){identifier}{spacesWithNl}*(?=(\({spacesWithNl}*[a-zA-Z0-9:{spacesWithNl},]*\){spacesWithNl}*\-\>))"));

        _lds.Insert(0, new Ld(Label, "[a-zA-Z_][a-zA-Z_0-9:]*:(?!([a-zA-Z0-9:]))"));
    }

    public static IReadonlyListWithIndexOf<Ld> LexemeDeclarations => _lds;
}