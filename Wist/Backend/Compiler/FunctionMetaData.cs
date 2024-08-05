namespace Wist.Backend.Compiler;

public record FunctionMetaData(string Name, IReadOnlyList<AsmValueType> Parameters, AsmValueType ReturnType);