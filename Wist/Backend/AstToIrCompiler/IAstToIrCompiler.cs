using Wist.Backend.IrToAsmCompiler.TypeSystem;
using Wist.Frontend.AstMaker;

namespace Wist.Backend.AstToIrCompiler;

public interface IAstToIrCompiler
{
    public IrImage Compile(AstNode root);
}

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

public enum IrType
{
    Add,
    Sub,
    Div,
    Mul,
    Mod,

    Push,
    Pop,
    Dup,
    Drop,
    Ret,
    LoadLocalValue,
    SetLocal,
    LoadReference,
    CallFunction,
    CallSharpFunction,

    CheckEquality,
    CheckInequality,
    CheckLessThan,
    CheckLessOrEquals,
    CheckGreaterThan,
    CheckGreaterOrEquals,

    Negate,
    DefineLabel,
    Br,
    BrFalse,

    GetReference,
}