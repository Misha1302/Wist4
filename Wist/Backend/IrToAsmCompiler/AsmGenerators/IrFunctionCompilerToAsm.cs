using System.Diagnostics;
using Iced.Intel;
using Wist.Backend.AstToIrCompiler;
using Wist.Backend.IrToAsmCompiler.AsmGenerators.CallingConventions;
using Wist.Backend.IrToAsmCompiler.Meta;
using Wist.Backend.IrToAsmCompiler.TypeSystem;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using InfoAboutMethod = (System.IntPtr ptr, System.Reflection.ParameterInfo[] parameters, System.Type returnType);

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public class IrFunctionCompilerToAsm(AstCompilerData data, IrFunction functionData)
{
    private const int PrologSizeInBytes = 16;
    private Dictionary<string, LocalInfo> _locals = null!;

    public void Compile(List<IrInstruction> instructions)
    {
        DefineLocalLabels();
        EmitFunctionDeclaration();
        foreach (var t in instructions)
            Emit(t);
    }

    private void DefineLocalLabels()
    {
        foreach (var label in functionData.GetLabels())
            data.Labels.Add(label, new LabelRef(data.Assembler.CreateLabel(label)));
    }

    private void EmitStack()
    {
        (_locals, var allocationBytes) = data.Helper.GetInfoAboutLocals(functionData.Locals);
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

    private void Emit(IrInstruction instruction)
    {
        data.DebugData.Add(data.Assembler.Instructions.Count, 4, instruction.ToString());

        switch (instruction.Instruction)
        {
            case IrType.Push:
                EmitTypeInstruction(instruction,
                    () =>
                    {
                        data.Assembler.mov(r14, instruction.Get<long>());
                        data.StackManager.Push(r14);
                    },
                    () =>
                    {
                        data.Assembler.mov(r14, instruction.Get<double>().As<double, long>());
                        data.Assembler.movq(xmm0, r14);
                        data.StackManager.Push(xmm0);
                    }
                );
                break;
            case IrType.Add:
                EmitAdd();
                break;
            case IrType.Sub:
                EmitSub();
                break;
            case IrType.Mul:
                EmitMul();
                break;
            case IrType.Div:
                EmitDiv();
                break;
            case IrType.Ret:
                StackOperate(() => data.StackManager.Pop(rax), () => data.StackManager.Pop(xmm0));
                EmitLeave();
                data.Assembler.ret();
                break;
            case IrType.LoadLocalValue:
                var localInfo = _locals[instruction.Get<string>()];
                EmitTypeInstruction(instruction,
                    () =>
                    {
                        data.Assembler.mov(r14, __[rbp - localInfo.Offset]);
                        data.StackManager.Push(r14);
                    },
                    () =>
                    {
                        data.Assembler.movq(xmm0, __[rbp - localInfo.Offset]);
                        data.StackManager.Push(xmm0);
                    }
                );
                break;
            case IrType.SetLocal:
                EmitSet(instruction);
                break;
            case IrType.CheckEquality:
                LogicOp(data.Assembler.cmove);
                break;
            case IrType.CheckLessThan:
                LogicOp(data.Assembler.cmovl);
                break;
            case IrType.CheckLessOrEquals:
                LogicOp(data.Assembler.cmovle);
                break;
            case IrType.CheckGreaterThan:
                LogicOp(data.Assembler.cmovg);
                break;
            case IrType.CheckGreaterOrEquals:
                LogicOp(data.Assembler.cmovge);
                break;
            case IrType.CheckInequality:
                LogicOp(data.Assembler.cmovne);
                break;
            case IrType.Negate:
                EmitNegation();
                break;
            case IrType.DefineLabel:
                data.DebugData.Add(data.Assembler.Instructions.Count, 4, $"{instruction.Get<string>()}:");
                data.Assembler.Label(ref data.Labels[instruction.Get<string>()].LabelByRef);
                data.Assembler.nop();
                break;
            case IrType.Br:
                data.Assembler.jmp(data.Labels[instruction.Get<string>()].LabelByRef);
                break;
            case IrType.Mod:
                EmitModulo();
                break;
            case IrType.BrFalse:
                data.StackManager.Pop(r14);
                data.Assembler.cmp(r14, 0);
                data.Assembler.je(data.Labels[instruction.Get<string>()].LabelByRef);
                break;
            default:
                throw new ArgumentOutOfRangeException(instruction.ToString());
        }
    }

    private void EmitTypeInstruction(IrInstruction instruction, Action i64, Action f64)
    {
        if (instruction.ValueTypeOfInstruction == AsmValueType.I64)
            i64();
        else if (instruction.ValueTypeOfInstruction == AsmValueType.F64)
            f64();
        else throw new InvalidOperationException();
    }

    private void EmitModulo()
    {
        data.Assembler.mov(rdx, 0);
        data.StackManager.Pop(r14);
        data.StackManager.Pop(rax);
        data.Assembler.idiv(r14);
        data.StackManager.Push(rdx);
    }

    private void EmitNegation()
    {
        data.StackManager.Pop(r14);
        data.Assembler.mov(r13, 0);
        data.Assembler.mov(r15, 1);

        data.Assembler.cmp(r14, 0);
        data.Assembler.cmove(r14, r15);
        data.Assembler.cmovne(r14, r13);

        data.StackManager.Push(r14);
    }

    private void EmitAdd()
    {
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
    }

    private void EmitSub()
    {
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
    }

    private void EmitMul()
    {
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
    }

    private void EmitDiv()
    {
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
    }

    private void EmitIdentifier(AstNode node)
    {
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
    }

    private void EmitSet(IrInstruction instruction)
    {
        StackOperate(() =>
        {
            data.StackManager.Pop(r14);
            data.Assembler.mov(__[rbp - _locals[instruction.Get<string>()].Offset], r14);
        }, () =>
        {
            data.StackManager.Pop(xmm0);
            data.Assembler.movq(__[rbp - _locals[instruction.Get<string>()].Offset], xmm0);
        });
    }

    private void EmitFunctionDeclaration()
    {
        if (data.Labels[functionData.Name].LabelByRef.InstructionIndex >= 0) return;

        data.Assembler.Label(ref data.Labels[functionData.Name].LabelByRef);

        EmitEnter();
        EmitStack();

        data.Assembler.vzeroupper();

        EmitParameters();
    }

    private void EmitGetRef(AstNode node)
    {
        var referenceIdentifier = node.Children[0].Lexeme.Text;
        if (_locals.TryGetValue(referenceIdentifier, out var local))
            data.Assembler.lea(r14, __[rbp - local.Offset]);
        else if (data.Labels.TryGetValue(referenceIdentifier, out var labelRef))
            data.Assembler.lea(r14, __[labelRef.LabelByRef]);
        else if (data.DllsManager.HasFunction(referenceIdentifier))
            data.Assembler.mov(r14, data.DllsManager.GetPointerOf(referenceIdentifier).ptr);
        else throw new InvalidOperationException();

        data.StackManager.Push(r14);
    }

    private void EmitFunctionCall(AstNode node)
    {
        // var funcName = node.Lexeme.Text;
        //
        // if (data.Labels.TryGetValue(funcName, out var funcLabel))
        // {
        //     var startSp = data.StackManager.Sp;
        //     var argsCount = node.Children[0].Children.Count;
        //     var presumablyEndSp = startSp + argsCount * 8;
        //     if (presumablyEndSp % 16 != 0) data.StackManager.PushStub();
        //
        //     var endSp = data.StackManager.Sp;
        //     var deltaSp = endSp - startSp;
        //
        //     Debug.Assert(data.StackManager.Sp % 16 == 0);
        //
        //     data.Assembler.call(funcLabel.LabelByRef);
        //
        //     data.StackManager.Drop(deltaSp);
        //
        //     var meta = data.Image.Functions.First(x => x.Name == funcName);
        //     if (meta.ReturnType == AsmValueType.Int64)
        //         data.StackManager.Push(rax);
        //     else if (meta.ReturnType == AsmValueType.Float64)
        //         data.StackManager.Push(xmm0);
        //     else throw new InvalidOperationException("Invalid return type");
        // }
        // else if (data.DllsManager.HasFunction(funcName))
        // {
        //     var funcToCall = data.DllsManager.GetPointerOf(funcName);
        //
        //     data.AstVisitor.Visit(node.Children[0], Emit, data.Helper.NeedToVisitChildren, true);
        //
        //     LoadArgumentsToRegisters(funcToCall);
        //
        //     var needToPushPopStub = data.StackManager.Sp % 16 != 0;
        //     if (needToPushPopStub) data.StackManager.PushStub();
        //
        //     DirectCall((ulong)funcToCall.ptr);
        //
        //     if (needToPushPopStub) data.StackManager.PopStub();
        //
        //     if (funcToCall.returnType == typeof(long))
        //         data.StackManager.Push(rax);
        //     else if (funcToCall.returnType == typeof(double))
        //         data.StackManager.Push(xmm0);
        //     else if (funcToCall.returnType != typeof(void))
        //         throw new InvalidOperationException("Invalid return type");
        // }
        // else
        // {
        //     throw new InvalidProgramException($"Has not found function with name {funcName}");
        // }
    }

    private void StackOperate(Action i64Action, Action f64Action)
    {
        var asmValueType = data.StackManager.Peek();
        if (asmValueType == AsmValueType.Int64) i64Action();
        else if (asmValueType == AsmValueType.Float64) f64Action();
        else throw new InvalidOperationException("Invalid type for this operation");
    }

    private void EmitParameters()
    {
        var offset = 0;

        foreach (var parameter in functionData.Parameters)
        {
            data.Assembler.mov(r14, __[rbp + PrologSizeInBytes + offset]);
            data.Assembler.mov(__[rbp - _locals[parameter.name].Offset], r14);
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
            var registers = CallConventions.SystemVAmd64Abi.ArgumentRegisters;

            for (var i = 0; i < funcToCall.parameters.Length; i++)
                if (funcToCall.parameters.Length >= i + 1)
                    if (data.StackManager.Peek() == AsmValueType.Int64)
                        data.StackManager.Pop(registers[i].i64);
                    else data.StackManager.Pop(registers[i].f64);
        }
        else
        {
            var registers = CallConventions.MicrosoftX64.ArgumentRegisters;

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