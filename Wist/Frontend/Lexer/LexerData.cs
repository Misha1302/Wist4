namespace Wist.Frontend.Lexer;

using static Lexemes.LexemeType;
using Ld = Wist.Frontend.Lexer.Lexemes.LexemeDeclaration;

public static class LexerData
{
    public static List<Ld> GetLexemeDeclarations()
    {
        var lds = new List<Ld>
        {
            new(LeftPar, "\\("),
            new(RightPar, "\\)"),
            new(LeftBrace, "\\{"),
            new(RightBrace, "\\}"),
            new(LeftRectangle, "\\["),
            new(RightRectangle, "\\]"),
            new(LessOrEquals, @"\<\="),
            new(GreaterOrEquals, @"\>\="),
            new(NotEqual, "!="),
            new(Equal, "=="),
            new(Import, "import"),
            new(String, "\"[^\"]+\""),
            new(As, "as"),
            new(NativeType, "(int64|int32|int16|float64)"),
            new(Alias, "alias"),
            new(Is, "is"),
            new(Set, "="),
            new(Int32, "-?\\d+"),
            new(Int64, "-?\\d+l"),
            new(Dot, "\\."),
            new(Plus, "\\+"),
            new(Mul, "\\*"),
            new(Div, "/"),
            new(If, "if"),
            new(Elif, "elif"),
            new(Else, "else"),
            new(Ret, "ret"),
            new(Spaces, "[ \t]+"),
            new(NewLine, "\r?\n"),
            new(LessThan, "\\<"),
            new(GreaterThan, "\\>"),
            new(Comma, ","),
            new(Minus, @"\-"),
            new(Identifier, "[a-zA-Z_][a-zA-Z_0-9]*")
        };

        lds.Insert(0, new Ld(PointerType, $"{lds.Get(Identifier).Pattern}\\*"));
        lds.Insert(0, new Ld(FunctionCall, $"{lds.Get(Identifier).Pattern}(?=({lds.Get(LeftPar).Pattern}))"));
        lds.Insert(0, new Ld(Label, $"{lds.Get(Identifier).Pattern}:"));
        lds.Insert(0, new Ld(Goto, $"goto (?=({lds.Get(Identifier).Pattern}))"));

        return lds;
    }
}