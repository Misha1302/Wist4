using System.Diagnostics;
using Iced.Intel;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using InfoAboutMethod = (System.IntPtr ptr, System.Reflection.ParameterInfo[] parameters, System.Type returnType);

namespace Wist.Backend.Compiler;

public class FunctionAstCompilerToAsm(AstCompilerData data)
{
    private Dictionary<string, int> _locals = null!;
    private int _sp;

    public void Compile(AstNode root)
    {
        EmitMainLoop(root);
    }

    private void EmitStack(AstNode root)
    {
        (_locals, var allocationBytes) = data.Helper.GetInfoAboutLocals(root);
        data.Assembler.sub(rsp, allocationBytes);
    }

    private void EmitEnter()
    {
        // 'cause of stack alignment we need to push odd count of registers
        data.Assembler.push(rbp);
        data.Assembler.mov(rbp, rsp);
    }


    private void EmitLeave()
    {
        data.Assembler.mov(rsp, rbp);
        data.Assembler.pop(rbp);
    }

    private void EmitMainLoop(AstNode node)
    {
        Label notIf;
        switch (node.Lexeme.LexemeType)
        {
            case LexemeType.Int64:
                data.Assembler.mov(r14, long.Parse(node.Lexeme.Text));
                push(r14);
                break;
            case LexemeType.Plus:
                pop(r14);
                data.Assembler.add(__[rsp], r14);
                break;
            case LexemeType.Minus:
                pop(r14);
                data.Assembler.sub(__[rsp], r14);
                break;
            case LexemeType.Mul:
                pop(r14);
                pop(rax);
                data.Assembler.imul(r14);
                push(rax);
                break;
            case LexemeType.Div:
                data.Assembler.mov(rdx, 0);
                pop(r14);
                pop(rax);
                data.Assembler.idiv(r14);
                push(rax);
                break;
            case LexemeType.Ret:
                pop(rax);
                EmitLeave();
                data.Assembler.ret();
                break;
            case LexemeType.Identifier:
                if (node.Parent?.Lexeme.LexemeType == LexemeType.Set)
                {
                    // load variable reference
                    data.Assembler.mov(r14, rbp);
                    data.Assembler.sub(r14, _locals[node.Lexeme.Text]);
                    push(r14);
                }
                else
                {
                    // push the value of variable
                    data.Assembler.mov(r14, __[rbp - _locals[node.Lexeme.Text]]);
                    push(r14);
                }

                break;
            case LexemeType.Set:
                // left - address - r15, right - value -  r14
                pop(r14);
                pop(r15);
                data.Assembler.mov(__[r15], r14);
                break;
            case LexemeType.FunctionDeclaration:
                if (data.Labels[node.Lexeme.Text].LabelByRef.InstructionIndex >= 0) break;

                data.Assembler.Label(ref data.Labels[node.Lexeme.Text].LabelByRef);

                EmitEnter();
                EmitStack(node);
                data.AstVisitor.Visit(node, EmitMainLoop, data.Helper.NeedToVisitChildren);

                break;
            case LexemeType.FunctionCall:
                var funcName = node.Lexeme.Text;

                if (data.Labels.TryGetValue(funcName, out var funcLabel))
                {
                    var startSp = _sp;
                    var argsCount = node.Children[0].Children.Count;
                    var presumablyEndSp = startSp + argsCount * 8;
                    if (presumablyEndSp % 16 != 0) pushStub();

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);
                    var endSp = _sp;
                    var deltaSp = endSp - startSp;

                    Debug.Assert(_sp % 16 == 0);

                    data.Assembler.call(funcLabel.LabelByRef);

                    data.Assembler.add(rsp, deltaSp);

                    push(rax);
                }
                else if (data.DllsManager.HasFunction(funcName))
                {
                    var funcToCall = data.DllsManager.GetPointerOf(funcName);

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

                    LoadArgumentsToRegisters(funcToCall);

                    var needToPushPopStub = _sp % 16 != 0;
                    if (needToPushPopStub) pushStub();

                    data.Assembler.sub(rsp, 32);
                    Debug.Assert(_sp % 16 == 0);
                    data.Assembler.call((ulong)funcToCall.ptr);
                    data.Assembler.add(rsp, 32);

                    if (needToPushPopStub) popStub();

                    if (funcToCall.returnType != typeof(void)) push(rax);
                }
                else
                {
                    throw new InvalidProgramException($"Has not found function with name {funcName}");
                }

                break;
            case LexemeType.Equal:
                LogicOp(data.Assembler.cmove);
                break;
            case LexemeType.LessThan:
                LogicOp(data.Assembler.cmovl);
                break;
            case LexemeType.LessOrEquals:
                LogicOp(data.Assembler.cmovle);
                break;
            case LexemeType.GreaterThan:
                LogicOp(data.Assembler.cmovg);
                break;
            case LexemeType.GreaterOrEquals:
                LogicOp(data.Assembler.cmovge);
                break;
            case LexemeType.NotEqual:
                LogicOp(data.Assembler.cmovne);
                break;
            case LexemeType.For:
                /*
                int64 i = 0

                while_start:

                if (!(i < 10)) ( goto while_end )

                body

                i = i + 1

                goto while_start

                while_end:
                */

                var whileEnd = data.Assembler.CreateLabel("while_end");

                // int64 i = 0
                data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

                // while_start:
                var whileStart = data.Assembler.CreateLabel("while_start");
                data.Assembler.Label(ref whileStart);

                // if (i < 10 == false) ( goto while_end )
                notIf = data.Assembler.CreateLabel("while_start");

                // condition
                data.AstVisitor.Visit(node.Children[1], EmitMainLoop, data.Helper.NeedToVisitChildren);
                pop(r14);
                data.Assembler.cmp(r14, 0);
                data.Assembler.jne(notIf);

                data.Assembler.jmp(whileEnd);
                data.Assembler.Label(ref notIf);

                // body
                data.AstVisitor.Visit(node.Children[3], EmitMainLoop, data.Helper.NeedToVisitChildren);

                // i = i + 1
                data.AstVisitor.Visit(node.Children[2], EmitMainLoop, data.Helper.NeedToVisitChildren);

                // goto while_start
                data.Assembler.jmp(whileStart);

                data.Assembler.Label(ref whileEnd);
                break;
            case LexemeType.If:
                data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

                pop(r14);
                notIf = data.Assembler.CreateLabel("not_if");
                data.Assembler.cmp(r14, 0);
                data.Assembler.je(notIf);

                data.AstVisitor.Visit(node.Children[1], EmitMainLoop, data.Helper.NeedToVisitChildren);

                data.Assembler.Label(ref notIf);
                break;
            case LexemeType.Negation:
                pop(r14);
                data.Assembler.mov(r13, 0);
                data.Assembler.mov(r15, 1);

                data.Assembler.cmp(r14, 0);
                data.Assembler.cmove(r14, r15);
                data.Assembler.cmovne(r14, r13);

                push(r14);
                break;
            case LexemeType.Label:
                var labelName = node.Lexeme.Text[..^1]; // skip ':'
                data.Labels.Add(labelName, new LabelRef(data.Assembler.CreateLabel(labelName)));
                data.Assembler.Label(ref data.Labels[labelName].LabelByRef);
                break;
            case LexemeType.Goto:
                data.Assembler.jmp(data.Labels[node.Children[0].Lexeme.Text].LabelByRef);
                break;
            case LexemeType.Arrow:
            case LexemeType.Type:
            case LexemeType.Scope:
                break;
            case LexemeType.Import:
                data.DllsManager.Import(node.Children[0].Lexeme.Text[1..^1]);
                break;
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
            case LexemeType.Spaces:
            case LexemeType.NewLine:
            case LexemeType.Comma:
            default:
                throw new ArgumentOutOfRangeException(node.Lexeme.ToString());
        }
    }

    private void LoadArgumentsToRegisters(InfoAboutMethod funcToCall)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            if (funcToCall.parameters.Length >= 1) pop(rdi);
            if (funcToCall.parameters.Length >= 2) pop(rsi);
            if (funcToCall.parameters.Length >= 3) pop(rdx);
            if (funcToCall.parameters.Length >= 4) pop(rcx);
            if (funcToCall.parameters.Length >= 5) pop(r8);
            if (funcToCall.parameters.Length >= 6) pop(r9);
            if (funcToCall.parameters.Length >= 7) throw new InvalidOperationException("Too many arguments");
        }
        else
        {
            if (funcToCall.parameters.Length >= 1) pop(rcx);
            if (funcToCall.parameters.Length >= 2) pop(rdx);
            if (funcToCall.parameters.Length >= 3) pop(r8);
            if (funcToCall.parameters.Length >= 4) pop(r9);
            if (funcToCall.parameters.Length >= 5) throw new InvalidOperationException("Too many arguments");
        }
    }

    private void LogicOp(Action<AssemblerRegister64, AssemblerRegister64> asmOp)
    {
        pop(r14);
        pop(r15);
        data.Assembler.cmp(r15, r14);
        data.Assembler.mov(r14, 0);
        data.Assembler.mov(r15, 1);
        asmOp(r14, r15);
        push(r14);
    }

    // ReSharper disable once InconsistentNaming
    private void popStub()
    {
        _sp -= 8;
        data.Assembler.add(rsp, 8);
    }

    // ReSharper disable once InconsistentNaming
    private void pushStub()
    {
        _sp += 8;
        data.Assembler.sub(rsp, 8);
    }

    // ReSharper disable once InconsistentNaming
    private void pop(AssemblerRegister64 register)
    {
        _sp -= 8;
        data.Assembler.pop(register);
    }

    // ReSharper disable once InconsistentNaming
    private void push(AssemblerRegister64 register)
    {
        _sp += 8;
        data.Assembler.push(register);
    }
}