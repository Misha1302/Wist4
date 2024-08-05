using Wist.Backend.Compiler.TypeSystem;

namespace Wist.Backend.Compiler.Meta;

public record FunctionMetaData(string Name, IReadOnlyList<AsmValueType> Parameters, AsmValueType ReturnType);