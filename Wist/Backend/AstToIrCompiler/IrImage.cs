namespace Wist.Backend.AstToIrCompiler;

public record IrImage(List<IrFunction> Functions, List<string> ImportPaths);