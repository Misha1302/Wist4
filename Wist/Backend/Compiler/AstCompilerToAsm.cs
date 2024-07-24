namespace Wist.Backend.Compiler;

using Iced.Intel;
using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;
using static Frontend.Lexer.Lexemes.LexemeType;
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
    private readonly Dictionary<string, LabelRef> _labels = [];

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
                if (node.Lexeme.LexemeType == Set)
                    variablesSet.Add(node.Children[0].Lexeme.Text);
            },
            NeedToVisitChildren
        );
        _variables = variablesSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        _assembler.sub(rsp, (variablesSet.Count + variablesSet.Count % 2) * 8);
    }

    private static bool NeedToVisitChildren(AstNode node) =>
        node.Lexeme.LexemeType is not If and not Elif and not Else and not Goto;

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
            case Int64:
                _assembler.mov(r14, long.Parse(node.Lexeme.Text));
                push(r14);
                break;
            case Plus:
                pop(r14);
                _assembler.add(__[rsp], r14);
                break;
            case Minus:
                pop(r14);
                _assembler.sub(__[rsp], r14);
                break;
            case Mul:
                pop(r14);
                pop(rax);
                _assembler.imul(r14);
                push(rax);
                break;
            case Div:
                _assembler.mov(rdx, 0);
                pop(r14);
                pop(rax);
                _assembler.idiv(r14);
                push(rax);
                break;
            case Ret:
                pop(rax);
                EmitLeave();
                _assembler.ret();
                break;
            case Identifier:
                if (node.Parent?.Lexeme.LexemeType == Set)
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
            case Set:
                // left - address - r15, right - value -  r14
                pop(r14);
                pop(r15);
                _assembler.mov(__[r15], r14);
                break;
            case FunctionCall:
                if (node.Lexeme.Text is "writeI64" or "calloc" or "free")
                    pop(rcx);

                var needToPushPopStub = _sp % 16 != 0;
                if (needToPushPopStub) pushStub();

                _assembler.sub(rsp, 32);
                if (node.Lexeme.Text == "writeI64") _assembler.call(BuildinFunctions.WriteI64Ptr);
                else if (node.Lexeme.Text == "readI64") _assembler.call(BuildinFunctions.ReadI64Ptr);
                else if (node.Lexeme.Text == "calloc") _assembler.call(BuildinFunctions.CallocPtr);
                else if (node.Lexeme.Text == "free") _assembler.call(BuildinFunctions.FreePtr);
                else throw new InvalidOperationException($"Unknown function {node.Lexeme.Text}");
                _assembler.add(rsp, 32);

                if (needToPushPopStub) popStub();

                if (node.Lexeme.Text is "calloc" or "readI64") push(rax);

                break;
            case Equal:
                LogicOp(_assembler.cmove);
                break;
            case LessThan:
                LogicOp(_assembler.cmovl);
                break;
            case LessOrEquals:
                LogicOp(_assembler.cmovle);
                break;
            case GreaterThan:
                LogicOp(_assembler.cmovg);
                break;
            case GreaterOrEquals:
                LogicOp(_assembler.cmovge);
                break;
            case NotEqual:
                LogicOp(_assembler.cmovne);
                break;
            case If:
                _astVisitor.Visit(node.Children[0], EmitMainLoop, NeedToVisitChildren);

                pop(r14);
                var notIf = _assembler.CreateLabel("not_if");
                _assembler.cmp(r14, 0);
                _assembler.je(notIf);

                _astVisitor.Visit(node.Children[1], EmitMainLoop, NeedToVisitChildren);

                _assembler.Label(ref notIf);
                break;
            case Negation:
                pop(r14);
                _assembler.mov(r13, 0);
                _assembler.mov(r15, 1);

                _assembler.cmp(r14, 0);
                _assembler.cmove(r14, r15);
                _assembler.cmovne(r14, r13);

                push(r14);
                break;
            case LexemeType.Label:
                var labelName = node.Lexeme.Text[..^1]; // skip ':'
                _labels.Add(labelName, new LabelRef(_assembler.CreateLabel(labelName)));
                _assembler.Label(ref _labels[labelName].LabelByRef);
                break;
            case Goto:
                _assembler.jmp(_labels[node.Children[0].Lexeme.Text].LabelByRef);
                break;
            case NativeType:
            case Scope:
                break;
            case Import:
            case String:
            case As:
            case Alias:
            case Is:
            case PointerType:
            case LeftPar:
            case RightPar:
            case LeftBrace:
            case RightBrace:
            case Int32:
            case LeftRectangle:
            case RightRectangle:
            case Dot:
            case Elif:
            case Else:
            case Spaces:
            case NewLine:
            case Comma:
            default:
                throw new ArgumentOutOfRangeException(node.Lexeme.ToString());
        }
    }

    private void LogicOp(Action<AssemblerRegister64, AssemblerRegister64> asmOp)
    {
        pop(r14);
        pop(r15);
        _assembler.cmp(r15, r14);
        _assembler.mov(r14, 0);
        _assembler.mov(r15, 1);
        asmOp(r14, r15);
        push(r14);
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