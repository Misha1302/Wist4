using System.Globalization;
using Wist.Backend.Compiler;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;

namespace Wist.MiddleEnd;

public class BasicAstOptimizer(ILogger logger) : IAstOptimizer
{
    private readonly AstVisitor _astVisitor = new();
    private readonly AstOptimizerStatistics _statistics = new();

    public AstNode OptimizeAst(AstNode root)
    {
        DoWhileHaveChanges(root, () =>
        {
            Precompute(root);
            InlineLocals(root);
            RemoveUnnecessaryLocals(root);
            DoWhileHaveChanges(root, () => SimplifyUnnecessaryScopesInExpressions(root));
            _statistics.OptimizeCyclesCalledCount++;
        });

        MakeLogs(root);

        return root;
    }

    private void RemoveUnnecessaryLocals(AstNode root)
    {
        var knownLocalsSetters = new Dictionary<string, AstNode>();
        var usedLocals = new HashSet<string>();

        var curFunctionName = string.Empty;
        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == LexemeType.FunctionDeclaration)
            {
                curFunctionName = node.Lexeme.Text;
                return;
            }

            if (node.Lexeme.LexemeType == LexemeType.Identifier
                && node.Parent?.Lexeme.LexemeType != LexemeType.Set
                && knownLocalsSetters.ContainsKey(curFunctionName + "<>" + node.Lexeme.Text))
                usedLocals.Add(curFunctionName + "<>" + node.Lexeme.Text);

            if (node.Lexeme.LexemeType != LexemeType.Set) return;
            var first = node.Children[0];
            if (first.Children.Count != 1 || first.Children[0].Lexeme.LexemeType != LexemeType.Type) return;

            knownLocalsSetters.Add(curFunctionName + "<>" + first.Lexeme.Text, node);
        }, _ => true);

        var unusedLocalsSetters = knownLocalsSetters.Where(x => !usedLocals.Contains(x.Key));
        foreach (var unusedLocal in unusedLocalsSetters)
            unusedLocal.Value.Parent?.Children.Remove(unusedLocal.Value);
    }

    private void MakeLogs(AstNode root)
    {
        logger.Log($"{GetType().FullName} used {_statistics.OptimizeCyclesCalledCount} optimize cycles\n{root}");
    }

    private static void DoWhileHaveChanges(AstNode root, Action act)
    {
        int prevHashCode;
        do
        {
            prevHashCode = root.GetHashCode();
            act();
        } while (root.GetHashCode() != prevHashCode);
    }

    private void SimplifyUnnecessaryScopesInExpressions(AstNode root)
    {
        _astVisitor.Visit(root, node =>
        {
            if (!node.Lexeme.LexemeType.IsOperation()) return;

            for (var index = 0; index < node.Children.Count; index++)
            {
                var child = node.Children[index];
                if (child.Lexeme.LexemeType == LexemeType.Scope && child.Children.Count == 1)
                    node.Children[index] = node.Children[index].Children[0];
            }
        }, _ => true);
    }

    private void InlineLocals(AstNode root)
    {
        var knownLocals =
            new Dictionary<string, (AstNode value, int minNumberOfApplicability, int maxNumberOfApplicability)>();

        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType != LexemeType.Set) return;
            var localValue = node.Children[1];
            if (!localValue.Lexeme.LexemeType.IsConst()) return;
            if (knownLocals.TryGetValue(node.Children[0].Lexeme.Text, out var value))
            {
                if (value.maxNumberOfApplicability == int.MaxValue)
                    value.maxNumberOfApplicability = node.Number;
                return;
            }

            knownLocals.Add(node.Children[0].Lexeme.Text, (localValue, localValue.Number + 1, int.MaxValue));
        }, _ => true);

        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType != LexemeType.Identifier) return;
            if (!knownLocals.TryGetValue(node.Lexeme.Text, out var value)) return;
            if (node.Number < value.minNumberOfApplicability) return;
            if (node.Number > value.maxNumberOfApplicability) return;

            node.Children = value.value.Children;
            node.Lexeme = value.value.Lexeme;
        }, _ => true);
    }

    private void Precompute(AstNode root)
    {
        _astVisitor.Visit(root, node =>
        {
            if (node.Children.Count != 2) return;

            var left = node.Children[0];
            var right = node.Children[1];

            if (left.Lexeme.LexemeType is not LexemeType.Int64 and not LexemeType.Float64) return;
            if (right.Lexeme.LexemeType is not LexemeType.Int64 and not LexemeType.Float64) return;
            if (left.Lexeme.LexemeType != right.Lexeme.LexemeType)
                throw new InvalidOperationException("Types must equal each other");

            if (node.Lexeme.LexemeType == LexemeType.Plus)
                Operate(node, left, right, (a, b) => a + b, (a, b) => a + b);
            else if (node.Lexeme.LexemeType == LexemeType.Minus)
                Operate(node, left, right, (a, b) => a - b, (a, b) => a - b);
            else if (node.Lexeme.LexemeType == LexemeType.Mul)
                Operate(node, left, right, (a, b) => a * b, (a, b) => a * b);
            else if (node.Lexeme.LexemeType == LexemeType.Div)
                Operate(node, left, right, (a, b) => a / b, (a, b) => a / b);
        }, _ => true);

        return;

        void Operate(AstNode node, AstNode left, AstNode right, Func<long, long, long> longFunc,
            Func<double, double, double> doubleFunction)
        {
            var type = left.Lexeme.LexemeType;
            node.Lexeme.LexemeType = type;
            node.Lexeme.Text = type == LexemeType.Int64
                ? longFunc(Int0(), Int1()).ToString()
                : doubleFunction(Float0(), Float1()).ToString(CultureInfo.CurrentCulture);

            node.Children.Clear();

            long Int0()
            {
                return left.Lexeme.Text.ToLong();
            }

            long Int1()
            {
                return right.Lexeme.Text.ToLong();
            }

            double Float0()
            {
                return left.Lexeme.Text.ToDouble();
            }

            double Float1()
            {
                return right.Lexeme.Text.ToDouble();
            }
        }
    }
}