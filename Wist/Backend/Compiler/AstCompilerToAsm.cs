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
    private readonly DllsManager _dllsManager = new();

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
            _ => true
        );
        _variables = variablesSet.Select((x, i) => (x, (i + 1) * 8)).ToDictionary();
        _assembler.sub(rsp, (variablesSet.Count + variablesSet.Count % 2) * 8);
    }

    private static bool NeedToVisitChildren(AstNode node) =>
        node.Lexeme.LexemeType is not If and not Elif and not Else and not Goto and not For and not Import;

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
        Label notIf;
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
                var funcToCall = _dllsManager.GetPointerOf(node.Lexeme.Text);

                if (funcToCall.parameters.Length >= 1) pop(rcx);
                if (funcToCall.parameters.Length >= 2) pop(rdx);
                if (funcToCall.parameters.Length >= 3) pop(r8);
                if (funcToCall.parameters.Length >= 4) pop(r9);

                var needToPushPopStub = _sp % 16 != 0;
                if (needToPushPopStub) pushStub();

                _assembler.sub(rsp, 32);
                _assembler.call((ulong)funcToCall.ptr);
                _assembler.add(rsp, 32);

                if (needToPushPopStub) popStub();

                if (funcToCall.returnType != typeof(void)) push(rax);
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
            case For:
                /*
                int64 i = 0

                while_start:

                if (!(i < 10)) ( goto while_end )

                body

                i = i + 1

                goto while_start

                while_end:
                */

                var whileEnd = _assembler.CreateLabel("while_end");

                // int64 i = 0
                _astVisitor.Visit(node.Children[0], EmitMainLoop, NeedToVisitChildren);

                // while_start:
                var whileStart = _assembler.CreateLabel("while_start");
                _assembler.Label(ref whileStart);

                // if (i < 10 == false) ( goto while_end )
                notIf = _assembler.CreateLabel("while_start");

                // condition
                _astVisitor.Visit(node.Children[1], EmitMainLoop, NeedToVisitChildren);
                pop(r14);
                _assembler.cmp(r14, 0);
                _assembler.jne(notIf);

                _assembler.jmp(whileEnd);
                _assembler.Label(ref notIf);

                // body
                _astVisitor.Visit(node.Children[3], EmitMainLoop, NeedToVisitChildren);

                // i = i + 1
                _astVisitor.Visit(node.Children[2], EmitMainLoop, NeedToVisitChildren);

                // goto while_start
                _assembler.jmp(whileStart);

                _assembler.Label(ref whileEnd);
                break;
            case If:
                _astVisitor.Visit(node.Children[0], EmitMainLoop, NeedToVisitChildren);

                pop(r14);
                notIf = _assembler.CreateLabel("not_if");
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
            case Type:
            case Scope:
                break;
            case Import:
                _dllsManager.Import(node.Children[0].Lexeme.Text[1..^1]);
                break;
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