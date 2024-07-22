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

        var astMaker = new AbstractSyntaxTreeMaker(lexemes);
        var astRoot = astMaker.GetAstRoot();

        Console.WriteLine(astRoot);
        
        var astExecutor = new AstExecutor();
        astExecutor.Execute(astRoot);
    }
}