using Iced.Intel;
using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;

namespace Wist.Backend.Compiler;

using static LexemeType;

public class ProgramAstCompilerToAsm : IAstCompiler
{
    private readonly AstCompilerData _data;

    private readonly ILogger _logger;

    public ProgramAstCompilerToAsm(ILogger logger)
    {
        _logger = logger;

        var assembler = new Assembler(64);
        _data = new AstCompilerData(
            assembler, new AstVisitor(), new DllsManager(), [], new AstCompilerToAsmHelper(),
            new DebugData.DebugData(), new StackManager(assembler), new MetaData()
        );
    }

    public IExecutable Compile(AstNode root)
    {
        EmitImport(root);
        EmitFunctions(root);
        EmitStartPoint();
        EmitFunctionCodes(root);
        return GetExecutable();
    }

    private IExecutable GetExecutable()
    {
        return OS.IsLinux()
            ? new LinuxAsmExecutable(_data.Assembler, _data.DebugData, _logger)
            : OS.IsWindows()
                ? new WindowsAsmExecutable(_data.Assembler, _data.DebugData, _logger)
                : throw new InvalidOperationException("No supported executable for this OS");
    }

    private void EmitFunctionCodes(AstNode root)
    {
        _data.AstVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == FunctionDeclaration)
                new FunctionAstCompilerToAsm(_data).Compile(node);
        }, _ => true);
    }

    private void EmitStartPoint()
    {
        _data.Assembler.push(rbp);
        _data.Assembler.push(r12);
        _data.Assembler.push(r13);
        _data.Assembler.push(r14);
        _data.Assembler.push(r15);
        _data.Assembler.push(rdx);
        _data.Assembler.push(r15);
        _data.Assembler.mov(rbp, rsp);

        _data.Assembler.call(_data.Labels["main"].LabelByRef);

        _data.Assembler.mov(rsp, rbp);
        _data.Assembler.pop(r15);
        _data.Assembler.pop(rdx);
        _data.Assembler.pop(r15);
        _data.Assembler.pop(r14);
        _data.Assembler.pop(r13);
        _data.Assembler.pop(r12);
        _data.Assembler.pop(rbp);
        _data.Assembler.ret();
    }

    private void EmitFunctions(AstNode root)
    {
        _data.AstVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType != FunctionDeclaration) return;

            _data.Labels.Add(node.Lexeme.Text, new LabelRef(_data.Assembler.CreateLabel(node.Lexeme.Text)));
            var parameters = node.Children[0].Children
                .Where(x => x.Lexeme.LexemeType == Type)
                .Select(x => x.Lexeme.Text.ToAsmValueType()).ToList();
            var returnType = node.Children[2].Lexeme.Text.ToAsmValueType();
            _data.MetaData.AddFunction(new FunctionMetaData(node.Lexeme.Text, parameters, returnType));
        }, _ => true);
    }

    private void EmitImport(AstNode root)
    {
        _data.AstVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == Import)
                _data.DllsManager.Import(node.Children[0].Lexeme.Text[1..^1]);
        }, _ => true);
    }
}