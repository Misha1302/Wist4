namespace Wist.Lexer;

using Wist.Lexer.Lexemes;
using static Lexemes.LexemeType;

public static class LexerData
{
    public static List<LexemeDeclaration> GetLexemeDeclarations()
    {
        var lds = new List<LexemeDeclaration>
        {
            new(LeftPar, "\\("),
            new(RightPar, "\\)"),
            new(LeftBrace, "\\{"),
            new(RightBrace, "\\}"),
            new(LeftRectangle, "\\["),
            new(RightRectangle, "\\]"),
            new(Import, "import"),
            new(String, "\"[^\"]+\""),
            new(As, "as"),
            new(Identifier, "[a-zA-Z_][a-zA-Z_0-9]*"),
            new(Alias, "alias"),
            new(Is, "is"),
            new(NativeType, "(int64|int32|int16|double)"),
            new(Set, "="),
            new(Int32, "-?\\d+"),
            new(Int64, "-?\\d+l"),
            new(Dot, "\\."),
            new(Plus, "\\+"),
            new(Minus, "\\-"),
            new(Mul, "\\*"),
            new(Div, "/"),
            new(If, "if"),
            new(Elif, "elif"),
            new(Else, "else"),
            new(Ret, "ret"),
            new(Spaces, "[ \t]+"),
            new(NewLine, "\r?\n"),
            new(LessThan, "\\<"),
            new(LessOrEquals, @"\<\="),
            new(GreaterThen, "\\>"),
            new(GreaterOrEquals, @"\>\="),
            new(Equal, "="),
            new(NotEqual, "!="),
            new(Comma, ",")
        };

        lds.Insert(0, new LexemeDeclaration(Pointer, $"{lds.Get(NativeType).Pattern}\\*"));
        lds.Insert(0, new LexemeDeclaration(FunctionCall, $"{lds.Get(Identifier).Pattern}(?=({lds.Get(LeftPar).Pattern}))"));
        lds.Insert(0, new LexemeDeclaration(Label, $"{lds.Get(Identifier).Pattern}:"));
        lds.Insert(0, new LexemeDeclaration(Goto, $"goto (?=({lds.Get(Identifier).Pattern}))"));

        return lds;
    }
}