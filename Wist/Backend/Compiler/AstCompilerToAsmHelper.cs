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

    public (Dictionary<string, int> locals, int allocationBytes) GetInfoAboutLocals(AstNode root)
    {
        var localsSet = new HashSet<string>();
        _astVisitor.Visit(root, node =>
            {
                if (node.Lexeme.LexemeType == Identifier
                    && node.Parent?.Lexeme.LexemeType == Set
                    && node.Children.Count > 0
                   )
                    localsSet.Add(node.Lexeme.Text);

                if (node.Lexeme.LexemeType == Identifier
                    && node.Children.Count > 0
                    && node.Children[0].Lexeme.LexemeType == LexemeType.Type
                    && node.Parent?.Parent?.Lexeme.LexemeType == FunctionDeclaration
                   )
                    localsSet.Add(node.Lexeme.Text);
            },
            _ => true
        );

        var locals = localsSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        return (locals, (localsSet.Count + localsSet.Count % 2) * 8);
    }
}