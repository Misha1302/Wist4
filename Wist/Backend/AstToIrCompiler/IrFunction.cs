using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

public record IrFunction(
    string Name,
    List<IIrLocalInfo> Locals,
    List<IrRealLocalInfo> Parameters,
    List<IrInstruction> Instructions,
    AsmValueType ReturnType)
{
    public List<string> GetLabels()
    {
        var labels = Instructions
            .Where(x => x.Instruction == IrType.DefineLabel)
            .Select(x => x.Get<string>())
            .ToList();
        return labels;
    }

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters)}) -> {ReturnType}";
    }
}