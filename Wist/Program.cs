namespace Wist;

public static class Program
{
    public static void Main()
    {
        var source = File.ReadAllText("CodeExamples/Calc");
        Console.WriteLine(source);

        var lexer = new Lexer.Lexer(source);
        var lexemes = lexer.Lexeme();
        Console.WriteLine(string.Join("\n", lexemes));
    }
}

public class AbstractSyntaxTreeMaker { }

public class SemanticAnalyzer { }