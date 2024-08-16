namespace Wist.Backend.IrToAsmCompiler.TypeSystem;

public static class AsmValueTypeExtensions
{
    private static readonly Dictionary<string, AsmValueType> _typeAliases = new()
    {
        ["long"] = AsmValueType.Int64,
        ["double"] = AsmValueType.Float64,
        ["ptr"] = AsmValueType.Int64,
        ["i64"] = AsmValueType.Int64,
        ["f64"] = AsmValueType.Float64,
        ["none"] = AsmValueType.None,
    };

    private static readonly Dictionary<Type, AsmValueType> _sharpTypesToAsmTypes = new()
    {
        [typeof(long)] = AsmValueType.Int64,
        [typeof(double)] = AsmValueType.Float64,
        [typeof(void)] = AsmValueType.None,
    };

    public static AsmValueType ToAsmValueType(this string alias)
    {
        return _typeAliases[alias];
    }

    public static AsmValueType SharpTypeToAsmValueType(this Type type)
    {
        return _sharpTypesToAsmTypes[type];
    }

    public static bool IsReferenceType(this string s)
    {
        return s == "ptr";
    }
}