using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrInstruction(IrType Instruction, AsmValueType ValueTypeOfInstruction, object Parameter1 = null!)
{
    private string Postfix =>
        ValueTypeOfInstruction == AsmValueType.I64 ? ".I" :
        ValueTypeOfInstruction == AsmValueType.F64 ? ".F" :
        "";

    public T Get<T>()
    {
        return (T)Parameter1;
    }

    public override string ToString()
    {
        var s = $"{Instruction}{Postfix}";
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Parameter1 != null)
            s += $" {Parameter1}";
        return s;
    }
}