namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public static class PreprocessedAbstractSyntaxTreeMaker
{
    private static readonly LexemeType[][] _lexemeTypes =
    [
        [LexemeType.NativeType, LexemeType.PointerType],
        [LexemeType.Mul, LexemeType.Div],
        [LexemeType.Plus, LexemeType.Minus],
        [LexemeType.Set]
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
                        if (curNode.Children.Count != 0) continue;
                        curNode.AddAndRemove(astNodes, i - 1, i + 1);
                        i--;
                        break;
                    case LexemeType.If:
                    case LexemeType.Elif:
                    case LexemeType.Else:
                    case LexemeType.Label:
                    case LexemeType.Goto:
                    case LexemeType.Ret:
                    case LexemeType.Spaces:
                    case LexemeType.NewLine:
                    case LexemeType.Comma:
                    case LexemeType.LessThan:
                    case LexemeType.LessOrEquals:
                    case LexemeType.GreaterThen:
                    case LexemeType.GreaterOrEquals:
                    case LexemeType.Equal:
                    case LexemeType.NotEqual:
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