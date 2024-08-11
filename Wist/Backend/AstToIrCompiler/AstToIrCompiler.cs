using System.Text;
using Wist.Backend.IrToAsmCompiler;
using Wist.Backend.IrToAsmCompiler.AsmGenerators;
using Wist.Backend.IrToAsmCompiler.TypeSystem;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Statistics.Logger;
using static Wist.Backend.IrToAsmCompiler.TypeSystem.AsmValueType;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Backend.AstToIrCompiler;

public class AstToIrCompiler(ILogger logger) : IAstToIrCompiler
{
    private readonly CompilerHelper _helper = new();
    private readonly IrImage _image = new([], []);
    private readonly Dictionary<string, AsmValueType> _localTypes = new();
    private readonly ImprovedStack<AsmValueType> _stack = new();
    private readonly AstVisitor _visitor = new();

    private IrFunction Function => _image.Functions[^1];
    private List<IrInstruction> Instructions => Function.Instructions;

    public IrImage Compile(AstNode root)
    {
        _visitor.Visit(root, DefineLocal, _ => true);
        _visitor.Visit(root, HandleNode, _helper.NeedToVisitChildren);

        Log();

        return _image;
    }

    private void DefineLocal(AstNode node)
    {
        if (node.Lexeme.LexemeType != LexemeType.Type) return;
        if (node.Parent?.Lexeme.LexemeType != Identifier) return;
        _localTypes.Add(node.Parent.Lexeme.Text, node.Lexeme.Text.ToAsmValueType());
    }

    private void Log()
    {
        var logString = new StringBuilder();
        logString.Append("Imports: ");
        logString.AppendJoin(", ", _image.ImportPaths.Select(x => '"' + x + '"'));
        logString.Append('\n');

        logString.Append('\n');
        foreach (var function in _image.Functions)
        {
            logString.Append(function);
            logString.Append("\n    ");
            logString.AppendJoin("\n    ", function.Instructions);
            logString.Append('\n');
        }

        logString.Remove(logString.Length - 1, 1);
        logger.Log(logString.ToString());
    }

    private void HandleNode(AstNode node)
    {
        var text = node.Lexeme.Text;
        switch (node.Lexeme.LexemeType)
        {
            case Identifier:
                if (node.Parent?.Lexeme.LexemeType != Set)
                    Instructions.Add(new IrInstruction(IrType.LoadLocalValue, _localTypes[text], text));
                break;
            case Set:
                Instructions.Add(new IrInstruction(IrType.SetLocal, _localTypes[node.Children[0].Lexeme.Text],
                    node.Children[0].Lexeme.Text));
                break;
            case Import:
                // remove "
                _image.ImportPaths.Add(node.Children[0].Lexeme.Text[1..^1]);
                break;
            case FunctionDeclaration:
                var parameters = node.Children[0].Children.Select(
                    x => (
                        name: x.Lexeme.Text,
                        type: x.Children[0].Lexeme.Text.ToAsmValueType()
                    )
                ).ToList();
                var returnType = node.Children[2].Lexeme.Text.ToAsmValueType();
                _image.Functions.Add(new IrFunction(text, [], parameters, [], returnType));
                _visitor.Visit(node.Children[3], HandleNode, _helper.NeedToVisitChildren);
                break;
            case LexemeType.Int64:
                Instructions.Add(new IrInstruction(IrType.Push, I64, text.ToLong()));
                _stack.Push(I64);
                break;
            case Character:
                Instructions.Add(new IrInstruction(IrType.Push, I64, (long)text[0]));
                break;
            case LexemeType.Float64:
                Instructions.Add(new IrInstruction(IrType.Push, F64, text.ToDouble()));
                _stack.Push(F64);
                break;
            case Plus:
                Instructions.Add(new IrInstruction(IrType.Add, _stack.Pop2AndPush1Same()));
                break;
            case Minus:
                Instructions.Add(new IrInstruction(IrType.Sub, _stack.Pop2AndPush1Same()));
                break;
            case Mul:
                Instructions.Add(new IrInstruction(IrType.Mul, _stack.Pop2AndPush1Same()));
                break;
            case Div:
                Instructions.Add(new IrInstruction(IrType.Div, _stack.Pop2AndPush1Same()));
                break;
            case Modulo:
                Instructions.Add(new IrInstruction(IrType.Mod, _stack.Pop2AndPush1Same()));
                break;
            case LessThan:
                Instructions.Add(new IrInstruction(IrType.CheckLessThan, _stack.Pop2AndPush1Same()));
                break;
            case LessOrEquals:
                Instructions.Add(new IrInstruction(IrType.CheckLessOrEquals, _stack.Pop2AndPush1Same()));
                break;
            case GreaterThan:
                Instructions.Add(new IrInstruction(IrType.CheckGreaterThan, _stack.Pop2AndPush1Same()));
                break;
            case GreaterOrEquals:
                Instructions.Add(new IrInstruction(IrType.CheckGreaterOrEquals, _stack.Pop2AndPush1Same()));
                break;
            case Equal:
                Instructions.Add(new IrInstruction(IrType.CheckEquality, _stack.Pop2AndPush1Same()));
                break;
            case NotEqual:
                Instructions.Add(new IrInstruction(IrType.CheckInequality, _stack.Pop2AndPush1Same()));
                break;
            case Ret:
                Instructions.Add(new IrInstruction(IrType.Ret, _stack.Pop()));
                break;
            case LexemeType.Type:
                if (node.Parent?.Lexeme.LexemeType == Identifier)
                    Function.Locals.Add((node.Parent.Lexeme.Text, text.ToAsmValueType()));
                break;
            case If:
                var ifLabel = $"if_false_{Guid.NewGuid()}";
                _visitor.Visit(node.Children[0], HandleNode, _helper.NeedToVisitChildren);
                Instructions.Add(new IrInstruction(IrType.BrFalse, Invalid, ifLabel));
                _visitor.Visit(node.Children[1], HandleNode, _helper.NeedToVisitChildren);
                Instructions.Add(new IrInstruction(IrType.DefineLabel, Invalid, ifLabel));
                break;
            case Negation:
                Instructions.Add(new IrInstruction(IrType.Negate, Invalid));
                break;
            case Goto:
                Instructions.Add(new IrInstruction(IrType.Br, Invalid, node.Children[0].Lexeme.Text));
                break;
            case Label:
                Instructions.Add(new IrInstruction(IrType.DefineLabel, Invalid, text[..^1]));
                break;
            case Scope:
            case Arrow:
            case Comment:
                break;
            case LexemeType.String:
            case As:
            case Alias:
            case Is:
            case FunctionCall:
            case LeftPar:
            case RightPar:
            case LeftBrace:
            case RightBrace:
            case LexemeType.Int32:
            case LeftRectangle:
            case RightRectangle:
            case Dot:
            case Elif:
            case Else:
            case Spaces:
            case NewLine:
            case Comma:
            case For:
            case GettingRef:
            default:
                throw new InvalidOperationException($"{node.Lexeme}");
        }
    }
}