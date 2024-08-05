using System.Diagnostics;
using System.Runtime.CompilerServices;
using Iced.Intel;
using Wist.Backend.Compiler.TypeSystem;

namespace Wist.Backend.Compiler.AsmGenerators;

public class StackManager(Assembler assembler)
{
    private readonly Stack<AsmValueType> _stackValueInfos = new();
    public int Sp => _stackValueInfos.Count * 8;

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void PopStub()
    {
        var asmValueType = _stackValueInfos.Pop();
        Debug.Assert(asmValueType == AsmValueType.None, "In stack was not none value");
        assembler.add(rsp, 8);
    }

    public void PushStub()
    {
        _stackValueInfos.Push(AsmValueType.None);
        assembler.sub(rsp, 8);
    }

    public AsmValueType Peek()
    {
        return _stackValueInfos.Peek();
    }

    public void Pop(AssemblerRegister64 register)
    {
        assembler.pop(register);
        var value = _stackValueInfos.Pop();
        Debug.Assert(value == AsmValueType.Int64);
    }

    public void Pop(AssemblerRegisterXMM register)
    {
        assembler.pop(rax);
        assembler.movq(register, rax);
        var value = _stackValueInfos.Pop();
        Debug.Assert(value == AsmValueType.Float64);
    }

    public void Push(AssemblerRegister64 register)
    {
        assembler.push(register);
        _stackValueInfos.Push(AsmValueType.Int64);
    }

    public void Push(AssemblerRegisterXMM register)
    {
        assembler.movq(rax, register);
        assembler.push(rax);
        _stackValueInfos.Push(AsmValueType.Float64);
    }

    public void Drop(int deltaSp)
    {
        Debug.Assert(deltaSp % 8 == 0);
        assembler.add(rsp, deltaSp);
        while (deltaSp > 0)
        {
            _stackValueInfos.Pop();
            deltaSp -= 8;
        }

        Debug.Assert(deltaSp == 0);
    }
}