namespace Wist;

using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer;

public static class Program
{
    public static void Main()
    {
        var source = File.ReadAllText("CodeExamples/Calc");
        Console.WriteLine(source);

        var lexer = new Lexer(source);
        var lexemes = lexer.Lexeme();
        Console.WriteLine(string.Join("\n", lexemes));

        var astMaker = new AbstractSyntaxTreeMaker(lexemes);
        Console.WriteLine(astMaker.GetAstRoot());
    }
}