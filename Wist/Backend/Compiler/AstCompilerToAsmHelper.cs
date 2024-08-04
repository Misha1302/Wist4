using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;

namespace Wist.Backend.Compiler;

public class AstCompilerToAsmHelper
{
    private readonly AstVisitor _astVisitor = new();

    public bool NeedToVisitChildren(AstNode node)
    {
        return node.Lexeme.LexemeType is not LexemeType.If and not LexemeType.Elif and not LexemeType.Else
            and not LexemeType.Goto and not LexemeType.For and not LexemeType.Import and not LexemeType.FunctionCall;
    }

    public (Dictionary<string, int> locals, int allocationBytes) GetInfoAboutLocals(AstNode root)
    {
        var localsSet = new HashSet<string>();
        _astVisitor.Visit(root, node =>
            {
                if (node.Lexeme.LexemeType == LexemeType.Identifier
                    && node.Parent?.Lexeme.LexemeType == LexemeType.Set
                    && node.Children.Count > 0
                   )
                    localsSet.Add(node.Lexeme.Text);

                if (node.Lexeme.LexemeType == LexemeType.Identifier
                    && node.Children.Count > 0
                    && node.Children[0].Lexeme.LexemeType == LexemeType.Type
                    && node.Parent?.Parent?.Lexeme.LexemeType == LexemeType.FunctionDeclaration
                   )
                    localsSet.Add(node.Lexeme.Text);
            },
            _ => true
        );

        var locals = localsSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        return (locals, (localsSet.Count + localsSet.Count % 2) * 8);
    }
}