﻿namespace Wist.Main;

using Wist.Backend.Compiler;
using Wist.Frontend;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer;
using Wist.Logger;
using Wist.MiddleEnd;

public static class Program
{
    public static void Main()
    {
        Execute();
    }

    private static void Execute()
    {
        var logger = new FileLogger();
        var sourceCodeReader = new SourceCodeReader(logger);
        var source = sourceCodeReader.Read("CodeExamples/Calc");

        var lexer = new Lexer(source, logger);
        var lexemes = lexer.Lexeme();

        var astMaker = new AbstractSyntaxTreeMaker(lexemes, logger);
        var astRoot = astMaker.GetAstRoot();

        var astOptimizer = new AstOptimizerStub();
        var optimizedRoot = astOptimizer.OptimizeAst(astRoot);

        var compiler = new AstCompilerToAsm(logger);
        var executable = compiler.Compile(optimizedRoot);

        File.WriteAllBytes("program.bin", executable.ToBinary());

        var result = executable.Execute();

        Console.WriteLine(result);
    }
}