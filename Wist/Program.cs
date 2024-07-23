namespace Wist;

using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer;
using Wist.Logger;

public static class Program
{
    public static void Main()
    {
        var logger = new FileLogger();
        var sourceCodeReader = new SourceCodeReader(logger);
        var source = sourceCodeReader.Read("CodeExamples/Calc");

        var lexer = new Lexer(source, logger);
        var lexemes = lexer.Lexeme();

        var astMaker = new AbstractSyntaxTreeMaker(lexemes, logger);
        var astRoot = astMaker.GetAstRoot();

        Console.WriteLine(astRoot);
    }
}