using Wist.Backend.Compiler.TypeSystem;

namespace Wist.Backend.Compiler.Meta;

public record LocalInfo(string Name, int Offset, AsmValueType Type);