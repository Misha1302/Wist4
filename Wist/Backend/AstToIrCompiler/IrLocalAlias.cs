using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrLocalAlias(string Alias, IIrLocalInfo RealLocalInfo) : IIrLocalInfo
{
    public string RealName => RealLocalInfo.Name;
    public string Name => Alias;
    public AsmValueType Type => RealLocalInfo.Type;
}