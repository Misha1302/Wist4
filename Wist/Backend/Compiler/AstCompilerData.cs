using Iced.Intel;
using Wist.Frontend.AstMaker;

namespace Wist.Backend.Compiler;

public record AstCompilerData(
    Assembler Assembler,
    AstVisitor AstVisitor,
    DllsManager DllsManager,
    Dictionary<string, LabelRef> Labels,
    AstCompilerToAsmHelper Helper
);