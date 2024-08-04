using Iced.Intel;
using Wist.Backend.Compiler.DebugData;

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

        for (var i = 0; i < asm.Instructions.Count; i++)
        {
            if (debugData.TryGet(i, out var list))
                foreach (var item in list)
                {
                    for (var j = 0; j < item.deepthLevel; j++)
                        so.Write("- ", FormatterTextKind.Text);
                    so.Write(item.message + "\n", FormatterTextKind.Text);
                }

            var instruction = asm.Instructions[i];
            formatter.Format(instruction, so);
            so.Write("\n", FormatterTextKind.Text);
        }

        return so.ToString()[..^1];
    }
}