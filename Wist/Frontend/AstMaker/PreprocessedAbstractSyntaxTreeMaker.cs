using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Wist.Frontend.Lexer.Lexemes;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Frontend.AstMaker;

public class PreprocessedAbstractSyntaxTreeMaker
{
    private static readonly LexemeType[][] _lexemeTypes =
    [
        [Goto, For, Import, FunctionDeclaration, FunctionCall, StructDeclaration, GettingRef, LexemeType.Type, Dot],
        [ReadMem],
        [Mul, Div, Modulo],
        [Plus, Minus],
        [Equal, NotEqual],
        [LessThan, GreaterThan, LessOrEquals, GreaterOrEquals],
        [Negation],
        [Set, WriteToMem],
        [Elif, Else],
        [If],
        [Ret],
    ];

    private readonly Dictionary<int, int> _astNodeLexemeTypeIndices = new(1024);
    private FrozenDictionary<int, int> _astNodeLexemeTypeIndicesFrozen = null!;

    private void PreprocessNodesToFastRecognition(List<AstNode> astNodes)
    {
        foreach (var t in astNodes)
        {
            PreprocessNodesToFastRecognition(t.Children);

            var targetLevel = -1;
            for (var level = 0; level < _lexemeTypes.Length; level++)
            {
                var lexemeTypes = _lexemeTypes[level];
                var ind = Array.IndexOf(lexemeTypes, t.Lexeme.LexemeType);
                if (ind < 0) continue;
                targetLevel = level;
                break;
            }

            _astNodeLexemeTypeIndices[t.Hash] = targetLevel;
        }
    }

    // 'cause method is too big, JIT don't want to optimize it (slow down = ~3 times). We need to point him to optimization
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void MakeOperationsNodes(List<AstNode> astNodes)
    {
        _astNodeLexemeTypeIndices.Clear();
        PreprocessNodesToFastRecognition(astNodes);
        _astNodeLexemeTypeIndicesFrozen = _astNodeLexemeTypeIndices.ToFrozenDictionary();
        MakeOperationsNodesInternal(astNodes, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void MakeOperationsNodesInternal(List<AstNode> astNodes, int lexemeIndex)
    {
        // ISSUE: very slow building deep nesting
        for (var i = 0; i < astNodes.Count; i++)
        {
            var curNode = astNodes[i];
            var handlingType = curNode.Lexeme.LexemeType;
            // var targetLexemeIndex = CollectionsMarshal.GetValueRefOrNullRef(_astNodeLexemeTypeIndices, curNode.Hash);
            if (_astNodeLexemeTypeIndicesFrozen[curNode.Hash] != lexemeIndex) continue;
            // if (!_lexemeTypes[lexemeIndex].Contains(curNode.Lexeme.LexemeType)) continue;

            i = MakeNode(astNodes, handlingType, i, curNode);
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < astNodes.Count; index++)
            MakeOperationsNodesInternal(astNodes[index].Children, lexemeIndex);

        if (lexemeIndex + 1 < _lexemeTypes.Length)
            MakeOperationsNodesInternal(astNodes, lexemeIndex + 1);
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
            case WriteToMem:
            case Set:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i - 1, i + 1);
                return i - 1;
            case FunctionDeclaration:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1, i + 2, i + 3, i + 4);
                curNode.Children[0].Children.RemoveAll(x => x.Lexeme.LexemeType == Comma);
                return i;
            case StructDeclaration:
                if (curNode.Children.Count != 0) return i;
                curNode.AddAndRemove(astNodes, i + 1, i + 2);
                return i;
            case Dot:
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
            case ReadMem:
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
            case RightBrace:
            case LeftBrace:
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
            case LexemeType.Int32:
            case LexemeType.Int64:
            case LeftRectangle:
            case RightRectangle:
            case Arrow:
            case Comment:
            case Float64:
            case Character:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}