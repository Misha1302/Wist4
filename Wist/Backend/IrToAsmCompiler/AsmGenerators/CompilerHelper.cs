using Wist.Backend.IrToAsmCompiler.Meta;
using Wist.Backend.IrToAsmCompiler.TypeSystem;
using Wist.Frontend.AstMaker;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public class CompilerHelper
{
    public bool NeedToVisitChildren(AstNode node)
    {
        return node.Lexeme.LexemeType is not If and not Elif and not Else and not Goto and not For and not Import
            and not FunctionCall and not GettingRef and not FunctionDeclaration;
    }

    public (Dictionary<string, LocalInfo> locals, int allocationBytes) GetInfoAboutLocals(
        List<(string name, AsmValueType type)> locals)
    {
        var localsInfo = locals.Select((x, i) => (x.name, new LocalInfo(x.name, (i + 1) * 8, x.type)));
        var localsCount = locals.Count + 1;
        var allocationBytes = (localsCount + localsCount % 2) * 8;
        return (localsInfo.ToDictionary(), allocationBytes);
    }
}