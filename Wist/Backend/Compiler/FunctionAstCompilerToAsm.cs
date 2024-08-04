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
        EmitLocalLabels(root);
        EmitMainLoop(root);
    }

    private void EmitLocalLabels(AstNode root)
    {
        data.AstVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType != LexemeType.Label) return;

            var lexemeText = node.Lexeme.Text[..^1];
            data.Labels.Add(lexemeText, new LabelRef(data.Assembler.CreateLabel(lexemeText)));
        }, _ => true);
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
        data.DebugData.Add(data.Assembler.Instructions.Count, node.GetScopeDepth(), node.Lexeme.ToString());

        switch (node.Lexeme.LexemeType)
        {
            case LexemeType.Int64:
                data.Assembler.mov(r14, long.Parse(node.Lexeme.Text.Replace("_", "")));
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
                EmitParameters(node);

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

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren, true);
                    var endSp = _sp;
                    var deltaSp = endSp - startSp;

                    Debug.Assert(_sp % 16 == 0);

                    data.Assembler.call(funcLabel.LabelByRef);

                    data.Assembler.add(rsp, deltaSp);
                    _sp -= deltaSp;

                    push(rax);
                }
                else if (data.DllsManager.HasFunction(funcName))
                {
                    var funcToCall = data.DllsManager.GetPointerOf(funcName);

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren, true);

                    LoadArgumentsToRegisters(funcToCall);

                    var needToPushPopStub = _sp % 16 != 0;
                    if (needToPushPopStub) pushStub();

                    DirectCall((ulong)funcToCall.ptr);

                    if (needToPushPopStub) popStub();

                    if (funcToCall.returnType != typeof(void)) push(rax);
                }
                else
                {
                    throw new InvalidProgramException($"Has not found function with name {funcName}");
                }

                break;
            case LexemeType.GettingRef:
                var referenceIdentifier = node.Children[0].Lexeme.Text;
                if (_locals.TryGetValue(referenceIdentifier, out var localOffset))
                    data.Assembler.lea(r14, __[rbp - localOffset]);
                else if (data.Labels.TryGetValue(referenceIdentifier, out var labelRef))
                    data.Assembler.lea(r14, __[labelRef.LabelByRef]);
                else if (data.DllsManager.HasFunction(referenceIdentifier))
                    data.Assembler.mov(r14, data.DllsManager.GetPointerOf(referenceIdentifier).ptr);
                else throw new InvalidOperationException();

                push(r14);
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
                var whileEnd = data.Assembler.CreateLabel("while_end");

                // int64 i = 0
                data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

                // while_start:
                var whileStart = data.Assembler.CreateLabel("while_start");
                data.Assembler.Label(ref whileStart);

                // if (i < 10 == false) ( goto while_end )
                var notIf = data.Assembler.CreateLabel("while_start");

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
                var endIfLabel = data.Assembler.CreateLabel("endIf");

                EmitIfBlock(node, endIfLabel);

                data.Assembler.Label(ref endIfLabel);
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
                data.Assembler.Label(ref data.Labels[labelName].LabelByRef);
                break;
            case LexemeType.Goto:
                data.Assembler.jmp(data.Labels[node.Children[0].Lexeme.Text].LabelByRef);
                break;
            case LexemeType.Modulo:
                data.Assembler.mov(rdx, 0);
                pop(r14);
                pop(rax);
                data.Assembler.idiv(r14);
                push(rdx);
                break;
            case LexemeType.Arrow:
            case LexemeType.Type:
            case LexemeType.Scope:
            case LexemeType.Comment:
            case LexemeType.Comma:
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
            case LexemeType.Spaces:
            case LexemeType.NewLine:
            default:
                throw new ArgumentOutOfRangeException(node.Lexeme.ToString());
        }
    }

    private void EmitIfBlock(AstNode node, Label endIfLabel)
    {
        var elseBlock = data.Assembler.CreateLabel("elseBlock");

        data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

        pop(r14);
        data.Assembler.cmp(r14, 0);
        data.Assembler.je(elseBlock);

        data.AstVisitor.Visit(node.Children[1], EmitMainLoop, data.Helper.NeedToVisitChildren);
        data.Assembler.jmp(endIfLabel);

        data.Assembler.Label(ref elseBlock);

        for (var i = 2; i < node.Children.Count; i++)
            if (node.Children[i].Lexeme.LexemeType == LexemeType.Elif)
                EmitIfBlock(node.Children[i], endIfLabel);
            else if (node.Children[i].Lexeme.LexemeType == LexemeType.Else)
                data.AstVisitor.Visit(node.Children[i].Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);
            else throw new InvalidOperationException("Unknown condition type");
    }

    private void EmitParameters(AstNode node)
    {
        var offset = 0;
        var parameters = node.Children[0].Children;

        foreach (var parameter in parameters)
        {
            data.Assembler.mov(r14, __[rbp + 16 + offset]);
            data.Assembler.mov(__[rbp - _locals[parameter.Lexeme.Text]], r14);
            offset += 8;
        }
    }

    private void DirectCall(ulong ptr)
    {
        if (OS.IsWindows()) data.Assembler.sub(rsp, 32);
        Debug.Assert(_sp % 16 == 0);
        data.Assembler.call(ptr);
        if (OS.IsWindows()) data.Assembler.add(rsp, 32);
    }

    private void LoadArgumentsToRegisters(InfoAboutMethod funcToCall)
    {
        if (OS.IsLinux())
        {
            // System V AMD64 ABI
            // https://en.wikipedia.org/wiki/X86_calling_conventions
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
            // Microsoft x64 calling convention
            // https://en.wikipedia.org/wiki/X86_calling_conventions
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