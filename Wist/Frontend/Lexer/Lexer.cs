namespace Wist.Frontend.Lexer;

using System.Text.RegularExpressions;
using Wist.Frontend.Lexer.Lexemes;

public class Lexer(string source)
{
    private readonly List<LexemeDeclaration> _lexemeDeclarations = LexerData.GetLexemeDeclarations();

    public List<Lexeme> Lexeme()
    {
        var lexemes = new List<Lexeme>();
        var pos = 0;
        while (pos < source.Length)
        {
            var matches = _lexemeDeclarations
                .Select(x => (decl: x, match: Regex.Match(source[pos..], x.Pattern)))
                .Where(x => x.match is { Success: true, Index: 0 })
                .ToList();

            if (matches.Count == 0) throw new InvalidDataException();

            var match = matches.First();

            pos += match.match.Value.Length;
            lexemes.Add(new Lexeme(match.decl.LexemeType, match.match.Value));
        }

        lexemes.RemoveAll(x => x.LexemeType == LexemeType.Spaces);
        return lexemes;
    }
}