namespace Wist.Backend.AstToIrCompiler;

public enum IrType
{
    Invalid,

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
    Nop,
}