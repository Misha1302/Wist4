using Wist.Backend.Compiler.AsmGenerators;
using Wist.Backend.Executing;
using Wist.Frontend;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;
using Wist.MiddleEnd;
using Wist.Statistics.TimeStatistic;

namespace Wist.Main;

public static class Program
{
    private static ILogger _logger = null!;
    private static string _source = null!;
    private static List<Lexeme> _lexemes = null!;
    private static AstNode _astRoot = null!;
    private static AstNode _optimizedRoot = null!;
    private static IExecutable _executable = null!;
    private static TimeMeasurer _measurer = null!;

    public static void Main()
    {
        Execute();
    }

    private static void Execute()
    {
        _logger = new FileLogger();
        _measurer = new TimeMeasurer(_logger);

        _measurer.Measure(ExecuteSourceCodeReader);
        _measurer.Measure(ExecuteLexer);
        _measurer.Measure(ExecuteAstMaker);
        _measurer.Measure(ExecuteAstOptimizer);
        _measurer.Measure(ExecuteAstCompiler);
        _measurer.Measure(ExecuteProgramSaver);
        _measurer.Measure(ExecuteExecutable);
    }

    private static void ExecuteExecutable()
    {
        var result = _executable.Execute();
        Console.WriteLine(result);
    }

    private static void ExecuteProgramSaver()
    {
        File.WriteAllBytes("program.bin", _executable.ToBinary());
    }

    private static void ExecuteAstCompiler()
    {
        var compiler = new ProgramAstCompilerToAsm(_logger);
        _executable = compiler.Compile(_optimizedRoot);
    }

    private static void ExecuteAstOptimizer()
    {
        var astOptimizer = new AstOptimizerStub();
        _optimizedRoot = astOptimizer.OptimizeAst(_astRoot);
    }

    private static void ExecuteAstMaker()
    {
        var astMaker = new AbstractSyntaxTreeMaker(_lexemes, _logger);
        _astRoot = astMaker.GetAstRoot();
    }

    private static void ExecuteLexer()
    {
        var lexer = new Lexer(_source, _logger);
        _lexemes = lexer.Lexeme();
    }

    private static void ExecuteSourceCodeReader()
    {
        var sourceCodeReader = new SourceCodeReader(_logger);
        _source = sourceCodeReader.Read("CodeExamples/Calc");
    }
}