namespace Wist.Backend.IrToAsmCompiler.TypeSystem;

public static class AsmValueTypeExtensions
{
    private static readonly Dictionary<string, AsmValueType> _typeAliases = new()
    {
        ["long"] = AsmValueType.Int64,
        ["double"] = AsmValueType.Float64,
        ["i64"] = AsmValueType.Int64,
        ["f64"] = AsmValueType.Float64,
    };

    public static AsmValueType ToAsmValueType(this string alias)
    {
        return _typeAliases[alias];
    }
}