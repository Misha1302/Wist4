using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrLocalAlias(string Alias, List<IIrLocalInfo> RealLocalsInfo) : IIrLocalInfo
{
    public string RealName => RealLocalsInfo[0].Name;
    public string Name => Alias;
    public AsmValueType Type => RealLocalsInfo[0].Type;
}