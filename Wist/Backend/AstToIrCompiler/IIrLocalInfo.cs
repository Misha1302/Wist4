using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public interface IIrLocalInfo
{
    public string Name { get; }
    public AsmValueType Type { get; }
}