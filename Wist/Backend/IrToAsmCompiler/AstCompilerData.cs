using Iced.Intel;
using Wist.Backend.IrToAsmCompiler.AsmGenerators;

namespace Wist.Backend.IrToAsmCompiler;

public record AstCompilerData(
    Assembler Assembler,
    DllsManager DllsManager,
    Dictionary<string, LabelRef> Labels,
    CompilerHelper Helper,
    DebugData.DebugData DebugData,
    StackManager StackManager
);