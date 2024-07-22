namespace Wist;

using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;

public class AstExecutor
{
    private readonly Stack<double> _stack = new();
    private string _varToSet = null!;

    private readonly Dictionary<string, double> _variables = new()
    {
        ["E"] = Math.E,
        ["Pi"] = Math.PI
    };

    private readonly List<(string[], Action)> _functions;

    public AstExecutor()
    {
        _functions = new List<(string[], Action)>
        {
            (["write", "writeln", "writeline", "print", "println"], () => Console.WriteLine(_stack.Pop())),
            (["sin"], () => _stack.Push(Math.Sin(_stack.Pop()))),
            (["cos"], () => _stack.Push(Math.Cos(_stack.Pop()))),
            (["tan"], () => _stack.Push(Math.Tan(_stack.Pop()))),
            (["cot"], () => _stack.Push(1.0 / Math.Tan(_stack.Pop())))
        };
    }

    public void Execute(AstNode astRoot)
    {
        double b;
        switch (astRoot.Lexeme.LexemeType)
        {
            case LexemeType.Int32:
                _stack.Push(int.Parse(astRoot.Lexeme.Text));
                break;
            case LexemeType.Plus:
                ExecuteChildren(astRoot);
                _stack.Push(_stack.Pop() + _stack.Pop());
                break;
            case LexemeType.Mul:
                ExecuteChildren(astRoot);
                _stack.Push(_stack.Pop() * _stack.Pop());
                break;
            case LexemeType.Minus:
                ExecuteChildren(astRoot);
                b = _stack.Pop();
                _stack.Push(_stack.Pop() - b);
                break;
            case LexemeType.Div:
                ExecuteChildren(astRoot);
                b = _stack.Pop();
                _stack.Push(_stack.Pop() / b);
                break;
            case LexemeType.FunctionCall:
                ExecuteChildren(astRoot);
                _functions.First(x => x.Item1.Contains(astRoot.Lexeme.Text)).Item2();
                break;
            case LexemeType.If:
                Execute(astRoot.Children[0]);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_stack.Pop() == double.Epsilon)
                {
                    Execute(astRoot.Children[1]);
                }
                else if (astRoot.Children.Count >= 3)
                {
                    var success = false;
                    foreach (var elif in astRoot.Children.Skip(2).SkipLast(1))
                    {
                        Execute(elif.Children[0]);
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (_stack.Pop() != double.Epsilon) continue;

                        Execute(elif);
                        success = true;
                        break;
                    }

                    if (!success)
                        Execute(astRoot.Children[^1]);
                }

                break;
            case LexemeType.Else:
                ExecuteChildren(astRoot);
                break;
            case LexemeType.Elif:
                ExecuteChildren(astRoot.Children[1]);
                break;
            case LexemeType.Equal:
                ExecuteChildren(astRoot);
                _stack.Push(Math.Abs(_stack.Pop() - _stack.Pop()) < 0.0001 ? double.Epsilon : 0);
                break;
            case LexemeType.NotEqual:
                ExecuteChildren(astRoot);
                _stack.Push(Math.Abs(_stack.Pop() - _stack.Pop()) >= 0.0001 ? double.Epsilon : 0);
                break;
            case LexemeType.LessThan:
                ExecuteChildren(astRoot);
                // ReSharper disable once EqualExpressionComparison
                _stack.Push(_stack.Pop() > _stack.Pop() ? double.Epsilon : 0);
                break;
            case LexemeType.GreaterThan:
                ExecuteChildren(astRoot);
                // ReSharper disable once EqualExpressionComparison
                _stack.Push(_stack.Pop() < _stack.Pop() ? double.Epsilon : 0);
                break;
            case LexemeType.LessOrEquals:
                ExecuteChildren(astRoot);
                // ReSharper disable once EqualExpressionComparison
                _stack.Push(_stack.Pop() >= _stack.Pop() ? double.Epsilon : 0);
                break;
            case LexemeType.GreaterOrEquals:
                ExecuteChildren(astRoot);
                // ReSharper disable once EqualExpressionComparison
                _stack.Push(_stack.Pop() <= _stack.Pop() ? double.Epsilon : 0);
                break;
            case LexemeType.Set:
                ExecuteChildren(astRoot);
                _variables[_varToSet] = _stack.Pop();
                break;
            case LexemeType.Identifier:
                if (astRoot.Parent?.Lexeme.LexemeType != LexemeType.Set)
                    _stack.Push(_variables[astRoot.Lexeme.Text]);
                else _varToSet = astRoot.Lexeme.Text;
                break;
            case LexemeType.Scope:
                ExecuteChildren(astRoot);
                break;
            case LexemeType.NewLine:
                break;
            default:
                throw new ArgumentOutOfRangeException(astRoot.Lexeme.LexemeType.ToString());
        }
    }

    private void ExecuteChildren(AstNode astRoot)
    {
        foreach (var child in astRoot.Children)
            Execute(child);
    }
}