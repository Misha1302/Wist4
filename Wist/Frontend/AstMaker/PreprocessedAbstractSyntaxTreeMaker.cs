using System.Runtime.CompilerServices;
using Wist.Frontend.Lexer.Lexemes;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Frontend.AstMaker;

public static class PreprocessedAbstractSyntaxTreeMaker
{
    private static readonly HashSet<LexemeType>[] _lexemeTypes =
    [
        [Goto, For, Import, FunctionDeclaration, FunctionCall, GettingRef, LexemeType.Type],
        [Mul, Div, Modulo],
        [Plus, Minus],
        [Equal, NotEqual],
        [LessThan, GreaterThan, LessOrEquals, GreaterOrEquals],
        [Negation],
        [Set],
        [Elif, Else],
        [If],
        [Ret],
    ];

    // 'cause method is too big, JIT don't want to optimize it (slow down = ~3 times). We need to point him to optimization
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void MakeOperationsNodes(List<AstNode> astNodes, int lexemeIndex)
    {
        var lexemeTypes = _lexemeTypes[lexemeIndex];

        for (var i = 0; i < astNodes.Count; i++)
        {
            var curNode = astNodes[i];
            var handlingType = curNode.Lexeme.LexemeType;
            if (!lexemeTypes.Contains(handlingType)) continue;

            i = MakeNode(astNodes, handlingType, i, curNode);
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < astNodes.Count; index++)
            MakeOperationsNodes(astNodes[index].Children, lexemeIndex);

        if (lexemeIndex + 1 < _lexemeTypes.Length)
            MakeOperationsNodes(astNodes, lexemeIndex + 1);
    }

    // 'cause method is too big, JIT don't want to optimize it (slow down = ~3 times). We need to point him to optimization
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int MakeNode(List<AstNode> astNodes, LexemeType handlingType, int i, AstNode curNode)
    {
        switch (handlingType)
        {
            case LexemeType.Type:
                if (astNodes.Count <= 1 || i + 1 >= astNodes.Count || astNodes[i + 1].Children.Count != 0) return i;
                astNodes[i + 1].AddAndRemove(astNodes, i);
                return i;
            case Set:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i - 1, i + 1);
                return i - 1;
            case FunctionDeclaration:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1, i + 2, i + 3, i + 4);
                curNode.Children[0].Children.RemoveAll(x => x.Lexeme.LexemeType == Comma);
                return i;
            case Modulo:
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
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i - 1, i + 1);
                return i - 1;
            case For:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1, i + 2, i + 3, i + 4);
                return i;
            case If:
                if (curNode.Children.Count != 0) return i;
                var indicesToInclude = (List<int>) [i + 1, i + 2];
                int j;
                for (j = 3;
                     i + j < astNodes.Count &&
                     astNodes[i + j].Lexeme.LexemeType is Else or Elif;
                     j++)
                    indicesToInclude.Add(i + j);

                curNode.AddAndRemove(astNodes, indicesToInclude.ToArray());
                return i;
            case Elif:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1, i + 2);
                return i;
            case Else:
            case Ret:
            case Negation:
            case Goto:
            case Import:
            case GettingRef:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1);
                return i;
            case FunctionCall:
                curNode.Children[0].Children.RemoveAll(x => x.Lexeme.LexemeType == Comma);
                return i;
            case Label:
                return i;
            case Spaces:
            case NewLine:
            case Comma:
            case Scope:
            case LexemeType.String:
            case As:
            case Identifier:
            case Alias:
            case Is:
            case LeftPar:
            case RightPar:
            case LeftBrace:
            case RightBrace:
            case LexemeType.Int32:
            case LexemeType.Int64:
            case LeftRectangle:
            case RightRectangle:
            case Dot:
            case Arrow:
            case Comment:
            case Float64:
            case Character:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}