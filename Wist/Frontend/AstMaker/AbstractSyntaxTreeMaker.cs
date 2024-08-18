using Wist.Frontend.Lexer.Lexemes;
using Wist.Statistics.Logger;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Frontend.AstMaker;

public class AbstractSyntaxTreeMaker(List<Lexeme> lexemes, ILogger logger)
{
    private AstNode _root = null!;

    public AstNode GetAstRoot()
    {
        // 1. when creating children, do not forget to set their parent
        // 2. the further away the type of operation is in _lexemeTypes,
        // the later it will process its children - this can set the order of operations

        _root = new AstNode(new Lexeme(Scope, "."), [], null, -1);

        MakeLinearNodes();
        MakeScopes(0, [(LeftPar, RightPar), (LeftBrace, RightBrace)]);
        MakeChildrenForFunctions(_root.Children);
        MakeOperationsNodes(_root.Children);
        logger.Log(_root.ToString());

        return _root;
    }

    private void MakeOperationsNodes(List<AstNode> rootChildren)
    {
        new PreprocessedAbstractSyntaxTreeMaker().MakeOperationsNodes(rootChildren);
    }

    private void MakeChildrenForFunctions(List<AstNode> nodes)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index].Children.Count != 0)
                MakeChildrenForFunctions(nodes[index].Children);

            if (index < 1 || nodes[index - 1].Lexeme.LexemeType != FunctionCall)
                continue;

            nodes[index - 1].Children.Add(nodes[index]);
            nodes[index].Parent = nodes[index - 1];
            nodes.RemoveAt(index);
        }
    }

    private void MakeScopes(int startIndex, List<(LexemeType left, LexemeType right)> brackets)
    {
        for (var i = startIndex; i < _root.Children.Count; i++)
        {
            var ind = brackets.FindIndex(x => x.left == _root.Children[i].Lexeme.LexemeType);
            if (ind < 0) continue;

            var pair = brackets[ind];

            _root.Children.RemoveAt(i);

            var children = new List<AstNode>();

            while (_root.Children[i].Lexeme.LexemeType != pair.right)
            {
                if (brackets.Any(x => x.left == _root.Children[i].Lexeme.LexemeType))
                    MakeScopes(i, brackets);

                children.Add(_root.Children[i]);

                _root.Children.RemoveAt(i);
            }

            _root.Children.RemoveAt(i);

            var scopeNode = new AstNode(new Lexeme(Scope, "."), children, _root,
                _root.Children[startIndex].Number);
            children.SetParent(scopeNode);

            _root.Children.Insert(i, scopeNode);
        }
    }

    private void MakeLinearNodes()
    {
        var i = 0;

        _root.Children.AddRange(lexemes.Select(x => new AstNode(x, [], _root, NextNumber())).ToList());

        return;

        int NextNumber()
        {
            return i++;
        }
    }
}