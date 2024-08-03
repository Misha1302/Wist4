using Wist.Frontend.Lexer.Lexemes;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Frontend.AstMaker;

public static class PreprocessedAbstractSyntaxTreeMaker
{
    private static readonly LexemeType[][] LexemeTypes =
    [
        [Goto, For, Import, FunctionDeclaration],
        [LexemeType.Type, PointerType],
        [Mul, Div],
        [Plus, Minus],
        [Equal, NotEqual],
        [LessThan, GreaterThan, LessOrEquals, GreaterOrEquals],
        [Negation],
        [Set],
        [Elif, Else],
        [If],
        [Ret],
    ];

    public static void MakeOperationsNodes(List<AstNode> astNodes, int lexemeIndex)
    {
        var lexemeTypes = LexemeTypes[lexemeIndex];

        for (var i = 0; i < astNodes.Count; i++)
        {
            var curNode = astNodes[i];
            var handlingType = curNode.Lexeme.LexemeType;
            if (lexemeTypes.Contains(handlingType))
                switch (handlingType)
                {
                    case LexemeType.Type:
                    case PointerType:
                        if (astNodes.Count <= 1 || astNodes[i + 1].Children.Count != 0) continue;
                        astNodes[i + 1].AddAndRemove(astNodes, i);
                        break;
                    case Set:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i - 1, i + 1);
                        i--;
                        break;
                    case FunctionDeclaration:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1, i + 2, i + 3, i + 4);
                        break;
                    case Minus:
                    case Div:
                    case Mul:
                    case Plus:
                    case Equal:
                    case NotEqual:
                    case LessThan:
                    case LessOrEquals:
                    case GreaterThan:
                    case GreaterOrEquals:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i - 1, i + 1);
                        i--;
                        break;
                    case For:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1, i + 2, i + 3, i + 4);
                        break;
                    case If:
                        if (curNode.Children.Count != 0) continue;
                        var indicesToInclude = (List<int>) [i + 1, i + 2];
                        int j;
                        for (j = 3;
                             i + j < astNodes.Count &&
                             astNodes[i + j].Lexeme.LexemeType is Else or Elif;
                             j++)
                            indicesToInclude.Add(i + j);

                        curNode.AddAndRemove(astNodes, indicesToInclude.ToArray());
                        break;
                    case Elif:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1, i + 2);
                        break;
                    case Else:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case Ret:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case Negation:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case Goto:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case Import:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case Label:
                        break;
                    case Spaces:
                    case NewLine:
                    case Comma:
                    case Scope:
                    case LexemeType.String:
                    case As:
                    case Identifier:
                    case Alias:
                    case Is:
                    case FunctionCall:
                    case LeftPar:
                    case RightPar:
                    case LeftBrace:
                    case RightBrace:
                    case LexemeType.Int32:
                    case LexemeType.Int64:
                    case LeftRectangle:
                    case RightRectangle:
                    case Dot:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        foreach (var children in astNodes.Select(x => x.Children))
            MakeOperationsNodes(children, lexemeIndex);

        if (lexemeIndex + 1 < LexemeTypes.Length)
            MakeOperationsNodes(astNodes, lexemeIndex + 1);
    }
}