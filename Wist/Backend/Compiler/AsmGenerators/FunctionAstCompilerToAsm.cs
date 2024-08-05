using System.Diagnostics;
using Iced.Intel;
using Wist.Backend.Compiler.Meta;
using Wist.Backend.Compiler.TypeSystem;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using InfoAboutMethod = (System.IntPtr ptr, System.Reflection.ParameterInfo[] parameters, System.Type returnType);

namespace Wist.Backend.Compiler.AsmGenerators;

public class FunctionAstCompilerToAsm(AstCompilerData data)
{
    private Dictionary<string, LocalInfo> _locals = null!;

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
        switch (node.Lexeme.LexemeType)
        {
            case LexemeType.Float64:
                data.Assembler.mov(r14, node.Lexeme.Text.ToDouble().As<double, long>());
                data.Assembler.movq(xmm0, r14);
                data.StackManager.Push(xmm0);
                break;
            case LexemeType.Int64:
                data.Assembler.mov(r14, node.Lexeme.Text.ToLong());
                data.StackManager.Push(r14);
                break;
            case LexemeType.Plus:
                StackOperate(() =>
                {
                    data.StackManager.Pop(r14);
                    data.Assembler.add(__[rsp], r14);
                }, () =>
                {
                    data.StackManager.Pop(xmm0);
                    data.StackManager.Pop(xmm1);
                    data.Assembler.addsd(xmm0, xmm1);
                    data.StackManager.Push(xmm0);
                });
                break;
            case LexemeType.Minus:
                StackOperate(() =>
                {
                    data.StackManager.Pop(r14);
                    data.Assembler.sub(__[rsp], r14);
                }, () =>
                {
                    data.StackManager.Pop(xmm0);
                    data.StackManager.Pop(xmm1);
                    data.Assembler.subsd(xmm1, xmm0);
                    data.StackManager.Push(xmm1);
                });
                break;
            case LexemeType.Mul:
                StackOperate(() =>
                {
                    data.StackManager.Pop(r14);
                    data.StackManager.Pop(rax);
                    data.Assembler.imul(r14);
                    data.StackManager.Push(rax);
                }, () =>
                {
                    data.StackManager.Pop(xmm0);
                    data.StackManager.Pop(xmm1);
                    data.Assembler.mulsd(xmm0, xmm1);
                    data.StackManager.Push(xmm0);
                });

                break;
            case LexemeType.Div:
                StackOperate(() =>
                {
                    data.Assembler.mov(rdx, 0);
                    data.StackManager.Pop(r14);
                    data.StackManager.Pop(rax);
                    data.Assembler.idiv(r14);
                    data.StackManager.Push(rax);
                }, () =>
                {
                    data.StackManager.Pop(xmm0);
                    data.StackManager.Pop(xmm1);
                    data.Assembler.divsd(xmm1, xmm0);
                    data.StackManager.Push(xmm1);
                });
                break;
            case LexemeType.Ret:
                StackOperate(() => data.StackManager.Pop(rax), () => data.StackManager.Pop(xmm0));
                EmitLeave();
                data.Assembler.ret();
                break;
            case LexemeType.Identifier:
                var localInfo = _locals[node.Lexeme.Text];
                if (node.Parent?.Lexeme.LexemeType == LexemeType.Set)
                {
                    // load variable reference
                    data.Assembler.mov(r14, rbp);
                    data.Assembler.sub(r14, localInfo.Offset);
                    data.StackManager.Push(r14);
                }
                else
                {
                    // push the value of variable
                    if (localInfo.Type == AsmValueType.Int64)
                    {
                        data.Assembler.mov(r14, __[rbp - localInfo.Offset]);
                        data.StackManager.Push(r14);
                    }
                    else if (localInfo.Type == AsmValueType.Float64)
                    {
                        data.Assembler.movq(xmm0, __[rbp - localInfo.Offset]);
                        data.StackManager.Push(xmm0);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                break;
            case LexemeType.Set:
                // left - address - r15, right - value -  r14
                StackOperate(() =>
                {
                    data.StackManager.Pop(r14);
                    data.StackManager.Pop(r15);
                    data.Assembler.mov(__[r15], r14);
                }, () =>
                {
                    data.StackManager.Pop(xmm0);
                    data.StackManager.Pop(r15);
                    data.Assembler.movq(__[r15], xmm0);
                });

                break;
            case LexemeType.FunctionDeclaration:
                if (data.Labels[node.Lexeme.Text].LabelByRef.InstructionIndex >= 0) break;

                data.Assembler.Label(ref data.Labels[node.Lexeme.Text].LabelByRef);

                EmitEnter();
                EmitStack(node);

                data.Assembler.vzeroupper();

                EmitParameters(node);

                data.AstVisitor.Visit(node, EmitMainLoop, data.Helper.NeedToVisitChildren);

                break;
            case LexemeType.FunctionCall:
                var funcName = node.Lexeme.Text;

                if (data.Labels.TryGetValue(funcName, out var funcLabel))
                {
                    var startSp = data.StackManager.Sp;
                    var argsCount = node.Children[0].Children.Count;
                    var presumablyEndSp = startSp + argsCount * 8;
                    if (presumablyEndSp % 16 != 0) data.StackManager.PushStub();

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren, true);
                    var endSp = data.StackManager.Sp;
                    var deltaSp = endSp - startSp;

                    Debug.Assert(data.StackManager.Sp % 16 == 0);

                    data.Assembler.call(funcLabel.LabelByRef);

                    data.StackManager.Drop(deltaSp);

                    var meta = data.MetaData.Functions.First(x => x.Name == funcName);
                    if (meta.ReturnType == AsmValueType.Int64)
                        data.StackManager.Push(rax);
                    else if (meta.ReturnType == AsmValueType.Float64)
                        data.StackManager.Push(xmm0);
                    else throw new InvalidOperationException("Invalid return type");
                }
                else if (data.DllsManager.HasFunction(funcName))
                {
                    var funcToCall = data.DllsManager.GetPointerOf(funcName);

                    data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren, true);

                    LoadArgumentsToRegisters(funcToCall);

                    var needToPushPopStub = data.StackManager.Sp % 16 != 0;
                    if (needToPushPopStub) data.StackManager.PushStub();

                    DirectCall((ulong)funcToCall.ptr);

                    if (needToPushPopStub) data.StackManager.PopStub();

                    if (funcToCall.returnType == typeof(long))
                        data.StackManager.Push(rax);
                    else if (funcToCall.returnType == typeof(double))
                        data.StackManager.Push(xmm0);
                    else if (funcToCall.returnType != typeof(void))
                        throw new InvalidOperationException("Invalid return type");
                }
                else
                {
                    throw new InvalidProgramException($"Has not found function with name {funcName}");
                }

                break;
            case LexemeType.GettingRef:
                var referenceIdentifier = node.Children[0].Lexeme.Text;
                if (_locals.TryGetValue(referenceIdentifier, out var local))
                    data.Assembler.lea(r14, __[rbp - local.Offset]);
                else if (data.Labels.TryGetValue(referenceIdentifier, out var labelRef))
                    data.Assembler.lea(r14, __[labelRef.LabelByRef]);
                else if (data.DllsManager.HasFunction(referenceIdentifier))
                    data.Assembler.mov(r14, data.DllsManager.GetPointerOf(referenceIdentifier).ptr);
                else throw new InvalidOperationException();

                data.StackManager.Push(r14);
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
                data.StackManager.Pop(r14);
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
                data.StackManager.Pop(r14);
                data.Assembler.mov(r13, 0);
                data.Assembler.mov(r15, 1);

                data.Assembler.cmp(r14, 0);
                data.Assembler.cmove(r14, r15);
                data.Assembler.cmovne(r14, r13);

                data.StackManager.Push(r14);
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
                data.StackManager.Pop(r14);
                data.StackManager.Pop(rax);
                data.Assembler.idiv(r14);
                data.StackManager.Push(rdx);
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

        data.DebugData.Add(data.Assembler.Instructions.Count, node.GetScopeDepth(), node.Lexeme.ToString());
    }

    private void StackOperate(Action i64Action, Action f64Action)
    {
        var asmValueType = data.StackManager.Peek();
        if (asmValueType == AsmValueType.Int64) i64Action();
        else if (asmValueType == AsmValueType.Float64) f64Action();
        else throw new InvalidOperationException("Invalid type for this operation");
    }

    private void EmitIfBlock(AstNode node, Label endIfLabel)
    {
        var elseBlock = data.Assembler.CreateLabel("elseBlock");

        data.AstVisitor.Visit(node.Children[0], EmitMainLoop, data.Helper.NeedToVisitChildren);

        data.StackManager.Pop(r14);
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
            data.Assembler.mov(__[rbp - _locals[parameter.Lexeme.Text].Offset], r14);
            offset += 8;
        }
    }

    private void DirectCall(ulong ptr)
    {
        if (OS.IsWindows()) data.Assembler.sub(rsp, 32);
        Debug.Assert(data.StackManager.Sp % 16 == 0);
        data.Assembler.call(ptr);
        if (OS.IsWindows()) data.Assembler.add(rsp, 32);
    }

    private void LoadArgumentsToRegisters(InfoAboutMethod funcToCall)
    {
        if (OS.IsLinux())
        {
            // System V AMD64 ABI
            // https://en.wikipedia.org/wiki/X86_calling_conventions

            var registers = (List<(AssemblerRegister64 i64, AssemblerRegisterXMM f64)>)
                [(rdi, xmm0), (rsi, xmm1), (rdx, xmm2), (rcx, xmm3), (r8, xmm4), (r9, xmm5)];

            for (var i = 0; i < funcToCall.parameters.Length; i++)
                if (funcToCall.parameters.Length >= i + 1)
                    if (data.StackManager.Peek() == AsmValueType.Int64)
                        data.StackManager.Pop(registers[i].i64);
                    else data.StackManager.Pop(registers[i].f64);
        }
        else
        {
            // Microsoft x64 calling convention
            // https://en.wikipedia.org/wiki/X86_calling_conventions
            var registers = (List<(AssemblerRegister64 i64, AssemblerRegisterXMM f64)>)
                [(rcx, xmm0), (rdx, xmm1), (r8, xmm2), (r9, xmm3)];

            for (var i = 0; i < funcToCall.parameters.Length; i++)
                if (funcToCall.parameters.Length >= i + 1)
                    if (data.StackManager.Peek() == AsmValueType.Int64)
                        data.StackManager.Pop(registers[i].i64);
                    else data.StackManager.Pop(registers[i].f64);
        }
    }

    private void LogicOp(Action<AssemblerRegister64, AssemblerRegister64> asmOp)
    {
        data.StackManager.Pop(r14);
        data.StackManager.Pop(r15);
        data.Assembler.cmp(r15, r14);
        data.Assembler.mov(r14, 0);
        data.Assembler.mov(r15, 1);
        asmOp(r14, r15);
        data.StackManager.Push(r14);
    }
}