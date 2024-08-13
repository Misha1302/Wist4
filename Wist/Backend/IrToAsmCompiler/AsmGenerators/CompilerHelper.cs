using Wist.Backend.AstToIrCompiler;
using Wist.Backend.IrToAsmCompiler.Meta;
using Wist.Frontend.AstMaker;
using static Wist.Frontend.Lexer.Lexemes.LexemeType;

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public class CompilerHelper
{
    public bool NeedToVisitChildren(AstNode node)
    {
        return node.Lexeme.LexemeType is not If and not Elif and not Else and not Goto and not For and not Import
            and not FunctionCall and not GettingRef and not FunctionDeclaration and not StructDeclaration;
    }

    public (Dictionary<string, LocalInfo> locals, int allocationBytes) GetInfoAboutLocals(List<IIrLocalInfo> locals)
    {
        var realLocals = locals.Where(x => x is not IrLocalAlias);
        var aliases = locals.Where(x => x is IrLocalAlias).Cast<IrLocalAlias>();

        var localsInfo = realLocals.Select((x, i) => (x.Name, new LocalInfo(x.Name, (i + 1) * 8, x.Type))).ToList();
        var aliasesInfo = aliases.Select(x => (x.Name, localsInfo.First(y => y.Name == x.RealName).Item2));
        localsInfo.AddRange(aliasesInfo);

        var localsCount = locals.Count + 1;
        var allocationBytes = (localsCount + localsCount % 2) * 8;
        var result = localsInfo.DistinctBy(x => x.Name).ToDictionary();
        return (result, allocationBytes);
    }
}