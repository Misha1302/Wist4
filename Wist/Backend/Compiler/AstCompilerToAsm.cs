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
    data transferring via stack

    r13, r14, r15 - temporally calculations
    */

    private Assembler _assembler = null!;
    private Dictionary<string, int> _variables = [];
    private int _sp;
    private readonly AstVisitor _astVisitor = new();

    public IExecutable Compile(AstNode root)
    {
        _assembler = new Assembler(64);
        EmitEnter();
        EmitStack(root);
        EmitBase();
        Emit(root);
        return new AsmExecutable(_assembler, logger);
    }

    private void EmitStack(AstNode root)
    {
        var variablesSet = new HashSet<string>();
        _astVisitor.Visit(root, node =>
            {
                if (node.Lexeme.LexemeType == LexemeType.Set)
                    variablesSet.Add(node.Children[0].Lexeme.Text);
            },
            NeedToVisitChildren
        );
        _variables = variablesSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        _assembler.sub(rsp, (variablesSet.Count + variablesSet.Count % 2) * 8);
    }

    private static bool NeedToVisitChildren(AstNode node) =>
        node.Lexeme.LexemeType is not LexemeType.If and not LexemeType.Elif and not LexemeType.Else;

    private void EmitEnter()
    {
        // 'cause of stack alignment we need to push odd count of registers
        _assembler.push(rbp);
        _assembler.push(r13);
        _assembler.push(r14);
        _assembler.push(r15);
        _assembler.push(r15);
        _assembler.mov(rbp, rsp);

        /*
        push 2 // 0
        push 4 // -8
        call x // -16

        x:
            push rbp // -24
            mov rbp, rsp

            // 16 - arg0, +8 - argX
            mov r10, [rbp + 16] // 4
            mov r11, [rbp + 24] // 2

            pop rbp
        */
    }


    private void EmitLeave()
    {
        _assembler.mov(rsp, rbp);
        _assembler.pop(r15);
        _assembler.pop(r15);
        _assembler.pop(r14);
        _assembler.pop(r13);
        _assembler.pop(rbp);
    }

    private void EmitBase()
    {
        _assembler.mov(r13, 0);
        _assembler.mov(r14, 0);
        _assembler.mov(r15, 0);
    }

    private void Emit(AstNode root)
    {
        _astVisitor.Visit(root, EmitMainLoop, NeedToVisitChildren);
    }

    private void EmitMainLoop(AstNode node)
    {
        switch (node.Lexeme.LexemeType)
        {
            case LexemeType.Int64:
                _assembler.mov(r14, long.Parse(node.Lexeme.Text));
                push(r14);
                break;
            case LexemeType.Plus:
                pop(r14);
                _assembler.add(__[rsp], r14);
                break;
            case LexemeType.Minus:
                pop(r14);
                _assembler.sub(__[rsp], r14);
                break;
            case LexemeType.Mul:
                pop(r14);
                pop(rax);
                _assembler.imul(r14);
                push(rax);
                break;
            case LexemeType.Div:
                _assembler.mov(rdx, 0);
                pop(r14);
                pop(rax);
                _assembler.idiv(r14);
                push(rax);
                break;
            case LexemeType.Ret:
                pop(rax);
                EmitLeave();
                _assembler.ret();
                break;
            case LexemeType.Identifier:
                if (node.Parent?.Lexeme.LexemeType == LexemeType.Set)
                {
                    // load variable reference
                    _assembler.mov(r14, rbp);
                    _assembler.sub(r14, _variables[node.Lexeme.Text]);
                    push(r14);
                }
                else
                {
                    // push the value of variable
                    _assembler.mov(r14, __[rbp - _variables[node.Lexeme.Text]]);
                    push(r14);
                }

                break;
            case LexemeType.Set:
                // left - address - r15, right - value -  r14
                pop(r14);
                pop(r15);
                _assembler.mov(__[r15], r14);
                break;
            case LexemeType.FunctionCall:
                if (node.Lexeme.Text is "writeI64" or "calloc" or "free")
                    pop(rcx);

                var needToPopStub = _sp % 16 != 0;
                if (_sp % 16 != 0) pushStub();

                _assembler.sub(rsp, 32);
                if (node.Lexeme.Text == "writeI64") _assembler.call(BuildinFunctions.WriteI64Ptr);
                else if (node.Lexeme.Text == "readI64") _assembler.call(BuildinFunctions.ReadI64Ptr);
                else if (node.Lexeme.Text == "calloc") _assembler.call(BuildinFunctions.CallocPtr);
                else if (node.Lexeme.Text == "free") _assembler.call(BuildinFunctions.FreePtr);
                else throw new InvalidOperationException($"Unknown function {node.Lexeme.Text}");
                _assembler.add(rsp, 32);

                if (needToPopStub) popStub();

                if (node.Lexeme.Text is "calloc" or "readI64") push(rax);

                break;
            case LexemeType.Equal:
                _assembler.pop(r14);
                _assembler.cmp(__[rsp], r14);
                _assembler.mov(r14, 0);
                _assembler.mov(r15, 1);
                _assembler.cmove(r14, r15);
                _assembler.push(r14);
                break;
            case LexemeType.If:
                _astVisitor.Visit(node.Children[0], EmitMainLoop, NeedToVisitChildren);

                _assembler.pop(r14);
                var notIf = _assembler.CreateLabel("not_if");
                _assembler.cmp(r14, 1);
                _assembler.jne(notIf);

                _astVisitor.Visit(node.Children[1], EmitMainLoop, NeedToVisitChildren);

                _assembler.Label(ref notIf);
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
            case LexemeType.LeftPar:
            case LexemeType.RightPar:
            case LexemeType.LeftBrace:
            case LexemeType.RightBrace:
            case LexemeType.Int32:
            case LexemeType.LeftRectangle:
            case LexemeType.RightRectangle:
            case LexemeType.Dot:
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
            case LexemeType.NotEqual:
            default:
                throw new ArgumentOutOfRangeException(node.Lexeme.ToString());
        }
    }

    // ReSharper disable once InconsistentNaming
    private void popStub()
    {
        _sp -= 8;
        _assembler.add(rsp, 8);
    }

    // ReSharper disable once InconsistentNaming
    private void pushStub()
    {
        _sp += 8;
        _assembler.sub(rsp, 8);
    }

    // ReSharper disable once InconsistentNaming
    private void pop(AssemblerRegister64 register)
    {
        _sp -= 8;
        _assembler.pop(register);
    }

    // ReSharper disable once InconsistentNaming
    private void push(AssemblerRegister64 register)
    {
        _sp += 8;
        _assembler.push(register);
    }
}