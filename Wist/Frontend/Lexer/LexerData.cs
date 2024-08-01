namespace Wist.Frontend.Lexer;

using static Lexemes.LexemeType;
using Ld = Lexemes.LexemeDeclaration;

public static class LexerData
{
    public static List<Ld> GetLexemeDeclarations()
    {
        var lds = new List<Ld>
        {
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
            new(Int32, "-?\\d+s"),
            new(Int64, "-?\\d+"),
            new(Dot, "\\."),
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
            new(Identifier, "[a-zA-Z_][a-zA-Z_0-9]*")
        };

        var identifier = lds.Get(Identifier).Pattern;
        lds.Insert(0, new Ld(PointerType, $"{identifier}\\*"));
        lds.Insert(0, new Ld(FunctionCall, $"{identifier}(?=({lds.Get(LeftPar).Pattern}))"));
        lds.Insert(0, new Ld(Label, $"{identifier}:"));
        lds.Insert(0, new Ld(Goto, $"goto (?=({identifier}))"));
        lds.Insert(0, new Ld(FunctionDeclaration, $@"{identifier}\s*(?=(\(\s*[a-zA-Z0-9\s]*\)\s*\-\>))"));
        
        var keywords = string.Join("|", lds.Where(x => x.Pattern.All(char.IsLetter)).Select(x => x.Pattern));
        string first = @$"(?<=[^a-zA-Z])(?!({keywords})){identifier}(?=(\s+{identifier}))";
        string second = @$"(?<=(\>\s*))(?!({keywords})){identifier}";
        lds.Insert(0, new Ld(Type, $"({first})|({second})"));

        return lds;
    }
}