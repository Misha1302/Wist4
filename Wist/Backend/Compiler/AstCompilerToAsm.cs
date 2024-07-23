namespace Wist.Backend.Compiler;

using Iced.Intel;
using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;
using AsmExecutable = Wist.Backend.Executing.AsmExecutable;

public class AstCompilerToAsm(ILogger logger) : IAstCompiler
{
    /*
    small note about microsoft x64 calling conv
    https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170

    rcx, rdx, r8, r9 - args
    xmm0, xmm1, xmm2, xmm3 - args also

    rax, r10, r11, xmm4, xmm5 - volatile


    in this compiler:
    r12 - stack pointer, r13 - stack offset, r14 - temporally calculations
    */

    private Assembler _assembler = null!;
    private readonly List<(Label, long)> _dataLabels = [];
    private Dictionary<string, int> _variables = [];

    public IExecutable Compile(AstNode root)
    {
        _assembler = new Assembler(64);
        EmitEnter();
        EmitStack(root);
        EmitBase();
        Emit(root);
        EmitData();
        return new AsmExecutable(_assembler, logger);
    }

    private void EmitStack(AstNode root)
    {
        var variablesSet = new HashSet<string>();
        new AstVisitor().Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == LexemeType.Set)
                variablesSet.Add(node.Children[0].Lexeme.Text);
        });
        _variables = variablesSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        _assembler.sub(rsp, (variablesSet.Count + variablesSet.Count % 2) * 8);
    }

    private void EmitEnter()
    {
        // 'cause of stack alignment we need to push odd count of registers
        _assembler.push(rbp);
        _assembler.push(r12);
        _assembler.push(r13);
        _assembler.push(r14);
        _assembler.push(r15);
        _assembler.mov(rbp, rsp);
    }


    private void EmitLeave()
    {
        _assembler.mov(rsp, rbp);
        _assembler.pop(r15);
        _assembler.pop(r14);
        _assembler.pop(r13);
        _assembler.pop(r12);
        _assembler.pop(rbp);
    }

    private void EmitData()
    {
        foreach (var tuple in _dataLabels)
        {
            var valueTuple = tuple;
            _assembler.Label(ref valueTuple.Item1);
            _assembler.dq(valueTuple.Item2);
        }
    }

    private void EmitBase()
    {
        _assembler.mov(rcx, 2048);
        _assembler.call(BuildinFunctions.CallocPtr);
        _assembler.mov(r12, rax);
        _assembler.mov(r13, 0);
        _assembler.mov(r14, 0);
    }

    private void Emit(AstNode root)
    {
        new AstVisitor().Visit(root, node =>
        {
            switch (node.Lexeme.LexemeType)
            {
                case LexemeType.Int64:
                    var label = _assembler.CreateLabel($"i64_{node.Lexeme.Text}");
                    _dataLabels.Add((label, long.Parse(node.Lexeme.Text)));
                    _assembler.mov(r14, __[label]);
                    _assembler.mov(__[r12 + r13], r14);
                    _assembler.add(r13, 8);
                    break;
                case LexemeType.Plus:
                    _assembler.mov(r14, __[r12 + r13 - 8]);
                    _assembler.add(__[r12 + r13 - 16], r14);
                    _assembler.sub(r13, 8);
                    break;
                case LexemeType.Minus:
                    _assembler.mov(r14, __[r12 + r13 - 8]);
                    _assembler.sub(__[r12 + r13 - 16], r14);
                    _assembler.sub(r13, 8);
                    break;
                case LexemeType.Mul:
                    _assembler.mov(rax, __[r12 + r13 - 16]);
                    _assembler.mov(r14, __[r12 + r13 - 8]);
                    _assembler.imul(r14);
                    _assembler.mov(__[r12 + r13 - 16], rax);
                    _assembler.sub(r13, 8);
                    break;
                case LexemeType.Div:
                    _assembler.mov(rdx, 0);
                    _assembler.mov(rax, __[r12 + r13 - 16]);
                    _assembler.mov(r14, __[r12 + r13 - 8]);
                    _assembler.idiv(r14);
                    _assembler.mov(__[r12 + r13 - 16], rax);
                    _assembler.sub(r13, 8);
                    break;
                case LexemeType.Ret:
                    _assembler.mov(rax, __[r12 + r13 - 8]);
                    EmitLeave();
                    _assembler.ret();
                    break;
                case LexemeType.Identifier:
                    if (node.Parent?.Lexeme.LexemeType == LexemeType.Set)
                    {
                        // push address of variable
                        _assembler.mov(r14, rbp);
                        _assembler.sub(r14, _variables[node.Lexeme.Text]);
                        _assembler.mov(__[r12 + r13], r14);
                        _assembler.adc(r13, 8);
                    }
                    else
                    {
                        // push the value of variable
                        _assembler.mov(r14, __[rbp - _variables[node.Lexeme.Text]]);
                        _assembler.mov(__[r12 + r13], r14);
                        _assembler.adc(r13, 8);
                    }

                    break;
                case LexemeType.Set:
                    // left - address - r15, right - value -  r14
                    _assembler.sub(r13, 8);
                    _assembler.mov(r14, __[r12 + r13]);
                    _assembler.sub(r13, 8);
                    _assembler.mov(r15, __[r12 + r13]);
                    _assembler.mov(__[r15], r14);
                    break;
                case LexemeType.NativeType:
                case LexemeType.Scope:
                    break;
                case LexemeType.Import:
                case LexemeType.String:
                case LexemeType.As:
                case LexemeType.Alias:
                case LexemeType.Is:
                case LexemeType.PointerType:
                case LexemeType.FunctionCall:
                case LexemeType.LeftPar:
                case LexemeType.RightPar:
                case LexemeType.LeftBrace:
                case LexemeType.RightBrace:
                case LexemeType.Int32:
                case LexemeType.LeftRectangle:
                case LexemeType.RightRectangle:
                case LexemeType.Dot:
                case LexemeType.If:
                case LexemeType.Elif:
                case LexemeType.Else:
                case LexemeType.Label:
                case LexemeType.Goto:
                case LexemeType.Spaces:
                case LexemeType.NewLine:
                case LexemeType.Comma:
                case LexemeType.LessThan:
                case LexemeType.LessOrEquals:
                case LexemeType.GreaterThan:
                case LexemeType.GreaterOrEquals:
                case LexemeType.Equal:
                case LexemeType.NotEqual:
                default:
                    throw new ArgumentOutOfRangeException(node.Lexeme.ToString());
            }
        });
    }
}