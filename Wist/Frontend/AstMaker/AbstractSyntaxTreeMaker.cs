namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public class AbstractSyntaxTreeMaker(List<Lexeme> lexemes)
{
    private readonly LexemeType[][] _lexemeTypes =
    [
        [LexemeType.NativeType, LexemeType.PointerType],
        [LexemeType.Mul, LexemeType.Div],
        [LexemeType.Plus, LexemeType.Minus],
        [LexemeType.Set]
    ];

    private AstNode _root = null!;

    public AstNode GetAstRoot()
    {
        // 1. when creating children, do not forget to set their parent
        // 2. the further away the type of operation is in _lexemeTypes,
        // the later it will process its children - this can set the order of operations

        _root = new AstNode(new Lexeme(LexemeType.Scope, "."), [], null);

        MakeLinearNodes();
        MakeParsScopes(0);
        MakeChildrenForFunctions(_root.Children);
        MakeOperationsNodes(_root.Children, 0);

        return _root;
    }

    private void MakeChildrenForFunctions(List<AstNode> nodes)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index].Children.Count != 0)
                MakeChildrenForFunctions(nodes[index].Children);

            if (index < 1 || nodes[index - 1].Lexeme.LexemeType != LexemeType.FunctionCall)
                continue;

            nodes[index - 1].Children.Add(nodes[index]);
            nodes[index].Parent = nodes[index - 1];
            nodes.RemoveAt(index);
        }
    }

    private void MakeOperationsNodes(List<AstNode> astNodes, int lexemeIndex)
    {
        var lexemeTypes = _lexemeTypes[lexemeIndex];

        for (var i = 0; i < astNodes.Count; i++)
        {
            var curNode = astNodes[i];
            var handlingType = curNode.Lexeme.LexemeType;
            if (lexemeTypes.Contains(handlingType))
                switch (handlingType)
                {
                    case LexemeType.Import:
                    case LexemeType.String:
                    case LexemeType.As:
                    case LexemeType.Identifier:
                    case LexemeType.Alias:
                    case LexemeType.Is:
                        break;
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        foreach (var children in astNodes.Select(x => x.Children))
            MakeOperationsNodes(children, lexemeIndex);

        if (lexemeIndex + 1 < _lexemeTypes.Length)
            MakeOperationsNodes(astNodes, lexemeIndex + 1);
    }

    private void MakeParsScopes(int startIndex)
    {
        for (var i = startIndex; i < _root.Children.Count; i++)
        {
            if (_root.Children[i].Lexeme.LexemeType != LexemeType.LeftPar) continue;

            _root.Children.RemoveAt(i);

            var children = new List<AstNode>();

            while (_root.Children[i].Lexeme.LexemeType != LexemeType.RightPar)
            {
                if (_root.Children[i].Lexeme.LexemeType == LexemeType.LeftPar)
                    MakeParsScopes(i);

                children.Add(_root.Children[i]);

                _root.Children.RemoveAt(i);
            }

            _root.Children.RemoveAt(i);

            var scopeNode = new AstNode(new Lexeme(LexemeType.Scope, "."), children, _root);
            children.SetParent(scopeNode);

            _root.Children.Insert(i, scopeNode);
        }
    }

    private void MakeLinearNodes()
    {
        _root.Children.AddRange(lexemes.Select(x => new AstNode(x, [], _root)).ToList());
    }
}