using System.Diagnostics;
using Iced.Intel;
using Wist.Backend.AstToIrCompiler;
using Wist.Backend.IrToAsmCompiler.AsmGenerators.CallingConventions;
using Wist.Backend.IrToAsmCompiler.Meta;
using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public class IrFunctionCompilerToAsm(AstCompilerData data, IrFunction functionData)
{
    private Dictionary<string, LocalInfo> _locals = null!;

    public void Compile(List<IrInstruction> instructions)
    {
        data.DebugData.Add(data.Assembler.Instructions.Count, 0, $"{functionData}");
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
            case IrType.ReadMem:
                data.StackManager.Pop(r14); // address

                if (instruction.ValueTypeOfInstruction == AsmValueType.I64)
                {
                    data.Assembler.mov(r15, __[r14]);
                    data.StackManager.Push(r15);
                }
                else
                {
                    data.Assembler.movq(xmm0, __[r14]);
                    data.StackManager.Push(xmm0);
                }

                break;
            case IrType.WriteToMem:
                if (instruction.ValueTypeOfInstruction == AsmValueType.I64)
                {
                    data.StackManager.Pop(r15); // value
                    data.StackManager.Pop(r14); // address
                    data.Assembler.mov(__[r14], r15);
                }
                else
                {
                    data.StackManager.Pop(xmm0); // value
                    data.StackManager.Pop(r14); // address
                    data.Assembler.movq(__[r14], xmm0);
                }

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
                LogicOp(data.Assembler.cmove, data.Assembler.cmpeqsd);
                break;
            case IrType.CheckLessThan:
                LogicOp(data.Assembler.cmovl, data.Assembler.cmpltsd);
                break;
            case IrType.CheckLessOrEquals:
                LogicOp(data.Assembler.cmovle, data.Assembler.cmplesd);
                break;
            case IrType.CheckGreaterThan:
                LogicOp(data.Assembler.cmovg, data.Assembler.cmpnlesd);
                break;
            case IrType.CheckGreaterOrEquals:
                LogicOp(data.Assembler.cmovge, data.Assembler.cmpnltsd);
                break;
            case IrType.CheckInequality:
                LogicOp(data.Assembler.cmovne, data.Assembler.cmpneqsd);
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
            case IrType.Drop:
                data.StackManager.Drop((int)instruction.Get<long>());
                break;
            case IrType.BrFalse:
                data.StackManager.Pop(r14);
                data.Assembler.cmp(r14, 0);
                data.Assembler.je(data.Labels[instruction.Get<string>()].LabelByRef);
                break;
            case IrType.GetReference:
                EmitGetRef(instruction.Get<string>());
                break;
            case IrType.Nop:
                data.Assembler.nop();
                break;
            case IrType.CallSharpFunction:
                var funcToCall = data.Image.DllsManager.GetPointerOf(instruction.Get<string>());
                Call(
                    funcToCall.parameters.Length,
                    funcToCall.returnType.SharpTypeToAsmValueType(),
                    () => DirectCall((ulong)funcToCall.ptr),
                    false,
                    (int)instruction.Get<long>(1)
                );
                break;
            case IrType.CallFunction:
                var irFunction =
                    data.Image.Functions.Find(x => x.Name == instruction.Get<string>())
                    ?? throw new InvalidOperationException();

                Call(
                    irFunction.Parameters.Count,
                    irFunction.ReturnType,
                    () => data.Assembler.call(data.Labels[irFunction.Name].LabelByRef),
                    true,
                    (int)instruction.Get<long>(1)
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(instruction.ToString());
        }
    }

    private void Call(int paramsCount, AsmValueType returnType, Action call, bool passArgumentsViaStack,
        int bytesToDrop)
    {
        if (!passArgumentsViaStack)
            LoadArgumentsToRegisters(paramsCount);

        var needToPushPopStub = data.StackManager.Sp % 16 != 0;
        if (needToPushPopStub) data.StackManager.PushStub();

        call();

        if (needToPushPopStub) data.StackManager.PopStub();
        data.StackManager.Drop(bytesToDrop);

        if (returnType == AsmValueType.I64)
            data.StackManager.Push(rax);
        else if (returnType == AsmValueType.F64)
            data.StackManager.Push(xmm0);
        else if (returnType != AsmValueType.None)
            throw new InvalidOperationException("Invalid return type");
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
            data.StackManager.Pop(xmm1);
            data.StackManager.Pop(xmm0);
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
            data.StackManager.Pop(xmm1);
            data.StackManager.Pop(xmm0);
            data.Assembler.subsd(xmm0, xmm1);
            data.StackManager.Push(xmm0);
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
            data.StackManager.Pop(xmm1);
            data.StackManager.Pop(xmm0);
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
            data.StackManager.Pop(xmm1);
            data.StackManager.Pop(xmm0);
            data.Assembler.divsd(xmm0, xmm1);
            data.StackManager.Push(xmm0);
        });
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

        CheckFunctionHaveReturn();

        data.Assembler.Label(ref data.Labels[functionData.Name].LabelByRef);

        EmitEnter();
        EmitStack();

        data.Assembler.vzeroupper();

        EmitParameters();
    }

    private void CheckFunctionHaveReturn()
    {
        if (functionData.Instructions.All(x => x.Instruction != IrType.Ret))
            throw new InvalidOperationException($"Function {functionData.Name} have not return");
    }

    private void EmitGetRef(string identifier)
    {
        if (_locals.TryGetValue(identifier, out var local))
            data.Assembler.lea(r14, __[rbp - local.Offset]);
        else if (data.Labels.TryGetValue(identifier, out var labelRef))
            data.Assembler.lea(r14, __[labelRef.LabelByRef]);
        else if (data.Image.DllsManager.HaveFunction(identifier))
            data.Assembler.mov(r14, data.Image.DllsManager.GetPointerOf(identifier).ptr);
        else throw new InvalidOperationException($"Unknown identifier {identifier} to get reference");

        data.StackManager.Push(r14);
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
        var paramsAllocBytes = (functionData.Parameters.Count + functionData.Parameters.Count % 2) * 8;
        data.Assembler.sub(rsp, paramsAllocBytes);

        for (var i = 0; i < functionData.Parameters.Count; i++)
        {
            var parameter = functionData.Parameters[i];
            data.Assembler.mov(r14, __[rbp + 16 + (functionData.Parameters.Count - i - 1) * 8]);
            data.Assembler.mov(__[rbp - _locals[parameter.Name].Offset], r14);
        }
    }

    private void DirectCall(ulong ptr)
    {
        if (OS.IsWindows()) data.Assembler.sub(rsp, 32);
        Debug.Assert(data.StackManager.Sp % 16 == 0);
        data.Assembler.call(ptr);
        if (OS.IsWindows()) data.Assembler.add(rsp, 32);
    }

    private void LoadArgumentsToRegisters(int argsCount)
    {
        if (OS.IsLinux())
        {
            var registers = CallConventions.SystemVAmd64Abi.ArgumentRegisters;

            for (var i = argsCount - 1; i >= 0; i--)
                if (data.StackManager.Peek() == AsmValueType.Int64)
                    data.StackManager.Pop(registers[i].i64);
                else data.StackManager.Pop(registers[i].f64);
        }
        else
        {
            var registers = CallConventions.MicrosoftX64.ArgumentRegisters;

            for (var i = argsCount - 1; i >= 0; i--)
                if (data.StackManager.Peek() == AsmValueType.Int64)
                    data.StackManager.Pop(registers[i].i64);
                else data.StackManager.Pop(registers[i].f64);
        }
    }

    private void LogicOp(
        Action<AssemblerRegister64, AssemblerRegister64> asmOpI64,
        Action<AssemblerRegisterXMM, AssemblerRegisterXMM> asmOpF64
    )
    {
        if (data.StackManager.Peek() == AsmValueType.I64)
        {
            data.StackManager.Pop(r14);
            data.StackManager.Pop(r15);
            data.Assembler.cmp(r15, r14);
            data.Assembler.mov(r14, 0);
            data.Assembler.mov(r15, 1);
            asmOpI64(r14, r15);
            data.StackManager.Push(r14);
        }
        else
        {
            // (r13 = cmp(xmm0, xmm1)) == 0 ? 0 : 1 

            data.StackManager.Pop(xmm0);
            data.StackManager.Pop(xmm1);
            asmOpF64(xmm0, xmm1);
            data.Assembler.movq(r13, xmm0);
            data.Assembler.mov(r14, 0); // result
            data.Assembler.mov(r15, 1);
            data.Assembler.cmp(r13, 0);
            data.Assembler.cmovne(r14, r15);
            data.StackManager.Push(r14);
        }
    }
}