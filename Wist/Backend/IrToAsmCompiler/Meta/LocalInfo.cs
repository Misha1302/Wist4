using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.IrToAsmCompiler.Meta;

public record LocalInfo(string Name, int Offset, AsmValueType Type);