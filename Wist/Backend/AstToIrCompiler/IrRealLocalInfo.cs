using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrRealLocalInfo(string Name, string TypeAsStr) : IIrLocalInfo
{
    public AsmValueType Type => TypeAsStr.ToAsmValueType();
}