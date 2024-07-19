namespace Wist;

public class Program
{
    public static void Main()
    {
        var lexer = new Lexer.Lexer(File.ReadAllText("CodeExamples/BinarySearch"));
        var lexemes = lexer.Lexeme();
        Console.WriteLine(string.Join("\n", lexemes));
    }
}

public class AbstractSyntaxTreeMaker { }

public class SemanticAnalyzer { }