namespace Wist.Backend.AstToIrCompiler;

public record IrStructureField(string Name, string TypeAsStr) : IrRealLocalInfo(Name, TypeAsStr);