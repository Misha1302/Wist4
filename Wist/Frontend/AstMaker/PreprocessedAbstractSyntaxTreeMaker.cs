namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public static class PreprocessedAbstractSyntaxTreeMaker
{
    private static readonly LexemeType[][] _lexemeTypes =
    [
        [LexemeType.NativeType, LexemeType.PointerType],
        [LexemeType.Mul, LexemeType.Div],
        [LexemeType.Plus, LexemeType.Minus],
        [LexemeType.Equal, LexemeType.NotEqual],
        [LexemeType.LessThan, LexemeType.GreaterThan, LexemeType.LessOrEquals, LexemeType.GreaterOrEquals],
        [LexemeType.Negation],
        [LexemeType.Set],
        [LexemeType.Elif, LexemeType.Else],
        [LexemeType.If],
        [LexemeType.Ret]
    ];

    public static void MakeOperationsNodes(List<AstNode> astNodes, int lexemeIndex)
    {
        var lexemeTypes = _lexemeTypes[lexemeIndex];

        for (var i = 0; i < astNodes.Count; i++)
        {
            var curNode = astNodes[i];
            var handlingType = curNode.Lexeme.LexemeType;
            if (lexemeTypes.Contains(handlingType))
                switch (handlingType)
                {
                    case LexemeType.NativeType:
                    case LexemeType.PointerType:
                        if (astNodes.Count <= 1 || astNodes[i + 1].Children.Count != 0) continue;
                        astNodes[i + 1].AddAndRemove(astNodes, i);
                        break;
                    case LexemeType.Set:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i - 1, i + 1);
                        i--;
                        break;
                    case LexemeType.Minus:
                    case LexemeType.Div:
                    case LexemeType.Mul:
                    case LexemeType.Plus:
                    case LexemeType.Equal:
                    case LexemeType.NotEqual:
                    case LexemeType.LessThan:
                    case LexemeType.LessOrEquals:
                    case LexemeType.GreaterThan:
                    case LexemeType.GreaterOrEquals:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i - 1, i + 1);
                        i--;
                        break;
                    case LexemeType.If:
                        if (curNode.Children.Count != 0) continue;
                        var indicesToInclude = (List<int>) [i + 1, i + 2];
                        int j;
                        for (j = 3;
                             i + j < astNodes.Count &&
                             astNodes[i + j].Lexeme.LexemeType is LexemeType.Else or LexemeType.Elif;
                             j++)
                            indicesToInclude.Add(i + j);

                        curNode.AddAndRemove(astNodes, indicesToInclude.ToArray());
                        break;
                    case LexemeType.Elif:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1, i + 2);
                        break;
                    case LexemeType.Else:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case LexemeType.Ret:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case LexemeType.Negation:
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i + 1);
                        break;
                    case LexemeType.Label:
                    case LexemeType.Goto:
                    case LexemeType.Spaces:
                    case LexemeType.NewLine:
                    case LexemeType.Comma:
                    case LexemeType.Scope:
                    case LexemeType.Import:
                    case LexemeType.String:
                    case LexemeType.As:
                    case LexemeType.Identifier:
                    case LexemeType.Alias:
                    case LexemeType.Is:
                    case LexemeType.FunctionCall:
                    case LexemeType.LeftPar:
                    case LexemeType.RightPar:
                    case LexemeType.LeftBrace:
                    case LexemeType.RightBrace:
                    case LexemeType.Int32:
                    case LexemeType.Int64:
                    case LexemeType.LeftRectangle:
                    case LexemeType.RightRectangle:
                    case LexemeType.Dot:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        foreach (var children in astNodes.Select(x => x.Children))
            MakeOperationsNodes(children, lexemeIndex);

        if (lexemeIndex + 1 < _lexemeTypes.Length)
            MakeOperationsNodes(astNodes, lexemeIndex + 1);
    }
}