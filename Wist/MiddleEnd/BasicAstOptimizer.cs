using System.Globalization;
using Wist.Backend.Compiler;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.MiddleEnd;

public class BasicAstOptimizer(ILogger logger) : IAstOptimizer
{
    private readonly AstVisitor _astVisitor = new();
    private readonly AstOptimizerStatistics _statistics = new();

    public AstNode OptimizeAst(AstNode root)
    {
        DoWhileHaveChanges(root, () =>
        {
            DoWhileHaveChanges(root, () =>
            {
                InlineLocals(root);
                SimplifyUnnecessaryScopesInExpressions(root);
            });
            RearrangeUnknownVariablesToHelpPrecomputations(root);
            Precompute(root);
            RemoveUnnecessaryLocals(root);

            _statistics.OptimizeCyclesCalledCount++;
        });

        MakeLogs(root);

        return root;
    }

    private void RearrangeUnknownVariablesToHelpPrecomputations(AstNode root)
    {
        _astVisitor.Visit(root, node =>
        {
            if (!node.Lexeme.LexemeType.IsOperation()) return;

            var chainType = node.Lexeme.LexemeType;
            if (chainType is not Mul and not Plus) return;

            var childType = node.Children[1].Lexeme.LexemeType;
            if (childType.IsConst() || childType.IsOperation()) return;


            var top = node;
            while (top.Parent?.Lexeme.LexemeType == chainType)
                top = top.Parent!;
            if (top == node) return;


            var lowerChild = node.Children[0];
            var parent = node.Parent!;
            lowerChild.Parent = parent;
            parent.Children[0] = lowerChild;


            top.Parent!.Children[^1] = node;
            node.Parent = top.Parent;
            node.Children[0] = top;
            top.Parent = node;
            _ = 5;
        }, _ => true);
    }

    private void RemoveUnnecessaryLocals(AstNode root)
    {
        var knownLocalsSetters = new Dictionary<string, AstNode>();
        var usedLocals = new HashSet<string>();

        var curFunctionName = string.Empty;
        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == FunctionDeclaration)
            {
                curFunctionName = node.Lexeme.Text;
                return;
            }

            if (node.Lexeme.LexemeType == Identifier
                && node.Parent?.Lexeme.LexemeType != Set
                && knownLocalsSetters.ContainsKey(curFunctionName + "<>" + node.Lexeme.Text))
                usedLocals.Add(curFunctionName + "<>" + node.Lexeme.Text);

            if (node.Lexeme.LexemeType != Set) return;
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
                if (child.Lexeme.LexemeType == Scope && child.Children.Count == 1)
                {
                    node.Children[index].Children[0].Parent = node.Children[index].Parent;
                    node.Children[index] = node.Children[index].Children[0];
                }
            }
        }, _ => true);
    }

    private void InlineLocals(AstNode root)
    {
        var knownLocals = new Dictionary<string, RefTuple<AstNode, int, int>>();
        RefTuple<AstNode, int, int> value;

        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == Identifier
                && node.Children.Count > 0
                && node.Children[0].Lexeme.LexemeType == LexemeType.Type
               )
                if (knownLocals.TryGetValue(node.Lexeme.Text, out value!))
                    if (value.Item3 == int.MaxValue)
                        value.Item3 = node.Number - 1;

            if (node.Lexeme.LexemeType != Set) return;
            var localValue = node.Children[1];
            if (!localValue.Lexeme.LexemeType.IsConst()) return;
            if (knownLocals.TryGetValue(node.Children[0].Lexeme.Text, out value!))
            {
                if (value.Item3 == int.MaxValue)
                    value.Item3 = node.Number;
                return;
            }

            knownLocals.Add(node.Children[0].Lexeme.Text,
                new RefTuple<AstNode, int, int>(localValue, localValue.Number + 1, int.MaxValue));
        }, _ => true);

        _astVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType != Identifier) return;
            if (!knownLocals.TryGetValue(node.Lexeme.Text, out var local)) return;
            if (node.Number < local.Item2) return;
            if (node.Number > local.Item3) return;

            node.Children = local.Item1.Children;
            node.Lexeme = local.Item1.Lexeme;
        }, _ => true);
    }

    private void Precompute(AstNode root)
    {
        _astVisitor.Visit(root, node =>
        {
            if (node.Children.Count != 2) return;

            var left = node.Children[0];
            var right = node.Children[1];

            if (left.Lexeme.LexemeType is not LexemeType.Int64 and not Float64) return;
            if (right.Lexeme.LexemeType is not LexemeType.Int64 and not Float64) return;
            if (left.Lexeme.LexemeType != right.Lexeme.LexemeType)
                throw new InvalidOperationException("Types must equal each other");

            if (node.Lexeme.LexemeType == Plus)
                Operate(node, left, right, (a, b) => a + b, (a, b) => a + b);
            else if (node.Lexeme.LexemeType == Minus)
                Operate(node, left, right, (a, b) => a - b, (a, b) => a - b);
            else if (node.Lexeme.LexemeType == Mul)
                Operate(node, left, right, (a, b) => a * b, (a, b) => a * b);
            else if (node.Lexeme.LexemeType == Div)
                Operate(node, left, right, (a, b) => a / b, (a, b) => a / b);
            else if (node.Lexeme.LexemeType == Modulo)
                Operate(node, left, right, (a, b) => a % b, (a, b) => a % b);
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
            return;

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