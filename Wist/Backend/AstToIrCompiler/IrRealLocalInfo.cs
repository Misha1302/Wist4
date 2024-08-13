using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrRealLocalInfo(
    string Name,
    AsmValueType Type
) : IIrLocalInfo;