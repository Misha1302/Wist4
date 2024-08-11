using Iced.Intel;
using Wist.Backend.AstToIrCompiler;
using Wist.Backend.IrToAsmCompiler.AsmGenerators;

namespace Wist.Backend.IrToAsmCompiler;

public record AstCompilerData(
    Assembler Assembler,
    Dictionary<string, LabelRef> Labels,
    CompilerHelper Helper,
    DebugData.DebugData DebugData,
    StackManager StackManager,
    IrImage Image
);