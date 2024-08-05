using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Backend.Compiler;

public class AstCompilerToAsmHelper
{
    private readonly AstVisitor _astVisitor = new();

    public bool NeedToVisitChildren(AstNode node)
    {
        return node.Lexeme.LexemeType is not If and not Elif and not Else and not Goto and not For and not Import
            and not FunctionCall and not GettingRef;
    }

    public (Dictionary<string, LocalInfo> locals, int allocationBytes) GetInfoAboutLocals(AstNode root)
    {
        var localsSet = new HashSet<(string, AsmValueType)>();
        _astVisitor.Visit(root, node =>
            {
                if (node.Lexeme.LexemeType != Identifier) return;
                if (node.Children.Count <= 0) return;
                if (node.Children[0].Lexeme.LexemeType != LexemeType.Type) return;

                localsSet.Add((node.Lexeme.Text, node.Children[0].Lexeme.Text.ToAsmValueType()));
            },
            _ => true
        );

        var locals = localsSet.Select((x, i) =>
            (x.Item1, new LocalInfo(x.Item1, (i + 1) * 8, x.Item2))
        ).ToDictionary();
        return (locals, (localsSet.Count + localsSet.Count % 2) * 8);
    }
}