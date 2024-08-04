using Iced.Intel;
using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;

namespace Wist.Backend.Compiler;

using static LexemeType;

public class ProgramAstCompilerToAsm(ILogger logger) : IAstCompiler
{
    private readonly AstCompilerData _data = new(
        new Assembler(64), new AstVisitor(), new DllsManager(), [], new AstCompilerToAsmHelper(), new DebugData()
    );

    public IExecutable Compile(AstNode root)
    {
        EmitImport(root);
        EmitFunctionLabels(root);
        EmitStartPoint();
        EmitFunctionCodes(root);
        return GetExecutable();
    }

    private IExecutable GetExecutable()
    {
        return OS.IsLinux()
            ? new LinuxAsmExecutable(_data.Assembler, _data.DebugData, logger)
            : new WindowsAsmExecutable(_data.Assembler, _data.DebugData, logger);
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

    private void EmitFunctionLabels(AstNode root)
    {
        _data.AstVisitor.Visit(root, node =>
        {
            if (node.Lexeme.LexemeType == FunctionDeclaration)
                _data.Labels.Add(node.Lexeme.Text, new LabelRef(_data.Assembler.CreateLabel(node.Lexeme.Text)));
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