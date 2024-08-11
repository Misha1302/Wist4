using Iced.Intel;
using Wist.Backend.IrToAsmCompiler.DebugData;

namespace Wist.Backend.Executing;

public static class AsmPrinter
{
    public static string PrintCodeToString(Assembler asm, IDebugData debugData)
    {
        var so = new StringOutput();
        var formatter = new NasmFormatter(new FormatterOptions
        {
            SpaceAfterOperandSeparator = true,
            NumberBase = NumberBase.Decimal,
            MemorySizeOptions = MemorySizeOptions.Default,
            DecimalDigitGroupSize = 3,
            DigitSeparator = "'",
        });

        var depthLevel = 0;
        for (var i = 0; i < asm.Instructions.Count; i++)
        {
            if (debugData.TryGet(i, out var list))
                foreach (var item in list)
                {
                    WriteDepthLevel(".#", item.depthLevel + 2, so, " ");
                    so.Write(item.message + "\n", FormatterTextKind.Text);

                    depthLevel = item.depthLevel;
                }

            var instruction = asm.Instructions[i];
            WriteDepthLevel("  ", depthLevel, so);
            formatter.Format(instruction, so);
            so.Write("\n", FormatterTextKind.Text);
        }

        return so.ToString()[..^1];
    }

    private static void WriteDepthLevel(string filler, int depthLevel, StringOutput so, string end = "")
    {
        for (var j = 0; j < depthLevel; j++)
            so.Write(filler, FormatterTextKind.Text);
        so.Write(end, FormatterTextKind.Text);
    }
}