namespace Wist.Frontend.AstMaker;

using Wist.Frontend.Lexer.Lexemes;

public class AbstractSyntaxTreeMaker(List<Lexeme> lexemes)
{
    private List<AstNode> _nodes = [];

    public AstNode GetAstRoot()
    {
        // построить много деревьев
        // по мере построений соединять в одно большой

        MakeLinearNodes();
        MakeParsScopes(0);

        var root = new AstNode(new Lexeme(LexemeType.Scope, "."), _nodes.ToList());
        return root;
    }

    private void MakeParsScopes(int startIndex)
    {
        for (var i = startIndex; i < _nodes.Count; i++)
        {
            if (_nodes[i].Lexeme.LexemeType != LexemeType.LeftPar) continue;
            i++;

            var children = new List<AstNode>();
            while (_nodes[i].Lexeme.LexemeType != LexemeType.RightPar)
            {
                if (_nodes[i].Lexeme.LexemeType == LexemeType.LeftPar)
                    MakeParsScopes(i);
                else children.Add(_nodes[i]);

                _nodes.RemoveAt(i);
            }

            _nodes.Insert(i, new AstNode(new Lexeme(LexemeType.Scope, "."), children));
        }
    }

    private void MakeLinearNodes()
    {
        _nodes = lexemes.Select(x => new AstNode(x, [])).ToList();
    }
}