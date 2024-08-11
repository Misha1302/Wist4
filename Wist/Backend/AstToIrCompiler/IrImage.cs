using Wist.Backend.IrToAsmCompiler;

namespace Wist.Backend.AstToIrCompiler;

public record IrImage(List<IrFunction> Functions, DllsManager DllsManager);