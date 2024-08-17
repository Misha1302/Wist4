using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public class IrInstruction(IrType instruction, AsmValueType valueTypeOfInstruction, params object[] parameters)
{
    public IrType Instruction = instruction;
    public object[] Parameters = parameters;
    public AsmValueType ValueTypeOfInstruction = valueTypeOfInstruction;

    private string Postfix =>
        ValueTypeOfInstruction == AsmValueType.I64 ? ".I" :
        ValueTypeOfInstruction == AsmValueType.F64 ? ".F" :
        "";

    public T Get<T>(int i = 0)
    {
        return (T)Parameters[i];
    }

    public override string ToString()
    {
        var s = $"{Instruction}{Postfix}";
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Parameters != null && Parameters.Length != 0)
            s += $" {string.Join(", ", Parameters)}";
        return s;
    }
}