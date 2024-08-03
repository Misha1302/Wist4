using Iced.Intel;

namespace Wist.Backend.Executing;

public static class AsmPrinter
{
    public static string PrintCodeToString(Assembler asm)
    {
        var so = new StringOutput();
        var formatter = new NasmFormatter(new FormatterOptions
        {
            SpaceAfterOperandSeparator = true,
            NumberBase = NumberBase.Decimal,
            MemorySizeOptions = MemorySizeOptions.Default,
            DecimalDigitGroupSize = 3,
            DigitSeparator = "'"
        });
        foreach (var i in asm.Instructions)
        {
            formatter.Format(i, so);
            so.Write("\n", FormatterTextKind.Text);
        }

        return so.ToString()[..^1];
    }
}