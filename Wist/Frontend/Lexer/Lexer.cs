using System.Text.RegularExpressions;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Statistics.Logger;

namespace Wist.Frontend.Lexer;

public class Lexer(string source, ILogger logger)
{
    private readonly List<LexemeDeclaration> _lexemeDeclarations = LexerData.GetLexemeDeclarations();

    public List<Lexeme> Lexeme()
    {
        var lexemes = new List<Lexeme>();
        var pos = 0;
        var regex = _lexemeDeclarations
            .Select(x => (decl: x, matches: Regex.Matches(source, x.Pattern)))
            .SelectMany(x => x.matches.Select(y => (x.decl, match: y)))
            .Where(x => x.match.Success)
            .OrderBy(x => x.match.Index)
            .ToList();

        var startIndex = 0;
        while (pos < source.Length)
        {
            startIndex = regex.FindIndex(startIndex, x => x.match.Index == pos);
            var match = regex[startIndex];

            if (match == default) throw new InvalidDataException();

            pos += match.match.Value.Length;
            lexemes.Add(new Lexeme(match.decl.LexemeType, match.match.Value));
        }

        lexemes.RemoveAll(x => x.LexemeType is LexemeType.Spaces or LexemeType.NewLine or LexemeType.Comment);
        logger.Log(string.Join("\n", lexemes));
        return lexemes;
    }
}