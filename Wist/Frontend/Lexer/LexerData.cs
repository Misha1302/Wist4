using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Frontend.Lexer;

using static LexemeType;
using Ld = LexemeDeclaration;

public static class LexerData
{
    public static List<Ld> GetLexemeDeclarations()
    {
        var lds = new List<Ld>
        {
            new(Comment, @"//[^\r?\n]*"),
            new(Arrow, "->"),
            new(LeftPar, "\\("),
            new(RightPar, "\\)"),
            new(LeftBrace, "\\{"),
            new(RightBrace, "\\}"),
            new(LeftRectangle, "\\["),
            new(RightRectangle, "\\]"),
            new(LessOrEquals, @"\<\="),
            new(GreaterOrEquals, @"\>\="),
            new(Negation, "!"),
            new(NotEqual, "!="),
            new(Equal, "=="),
            new(Import, "import"),
            new(String, "\"[^\"]+\""),
            new(As, "as"),
            new(Alias, "alias"),
            new(Is, "is"),
            new(Set, "="),
            new(Int64, @"-?\d+[\d_]*\d*"),
            new(Dot, "\\."),
            new(Modulo, "\\%"),
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
            new(Identifier, "[a-zA-Z_][a-zA-Z_0-9]*"),
        };

        var integer = lds.Get(Int64).Pattern;
        var identifier = lds.Get(Identifier).Pattern;
        var arrow = lds.Get(Arrow).Pattern;
        var keywords = string.Join("|", lds.Where(x => x.Pattern.All(char.IsLetter)).Select(x => x.Pattern));
        var first = @$"(?<=[^a-zA-Z])(?!({keywords})){identifier}(?=(\s+{identifier}))";
        var second = @$"(?<=({arrow}\s*))(?!({keywords})){identifier}";
        lds.Insert(0, new Ld(Type, $"({first})|({second})\\*?"));

        lds.Insert(0, new Ld(Int32, $"{integer}s"));
        lds.Insert(0, new Ld(Float64, integer + "\\." + integer));
        lds.Insert(0, new Ld(GettingRef, $"&(?=({identifier}))"));
        lds.Insert(0, new Ld(FunctionCall, $"{identifier}(?=({lds.Get(LeftPar).Pattern}))"));
        lds.Insert(0, new Ld(Label, $"{identifier}:"));
        lds.Insert(0, new Ld(Goto, $"goto (?=({identifier}))"));
        lds.Insert(0, new Ld(FunctionDeclaration, $@"{identifier}\s*(?=(\(\s*[a-zA-Z0-9\s,]*\)\s*\-\>))"));

        return lds;
    }
}