using Iced.Intel;
using Wist.Backend.Compiler.Meta;
using Wist.Backend.Compiler.TypeSystem;
using Wist.Backend.Executing;
using Wist.Frontend.AstMaker;
using Wist.Frontend.Lexer.Lexemes;
using Wist.Logger;

namespace Wist.Backend.Compiler.AsmGenerators;

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
        // need odd count
        _data.Assembler.push(rbp);
        _data.Assembler.push(r12);
        _data.Assembler.push(r13);
        _data.Assembler.push(r14);
        _data.Assembler.push(r15);
        _data.Assembler.push(rdx);
        _data.Assembler.push(r11);

        _data.Assembler.push(rdi);
        _data.Assembler.push(rsi);
        _data.Assembler.push(rdx);
        _data.Assembler.push(rcx);
        _data.Assembler.push(r8);
        _data.Assembler.push(r9);
        _data.Assembler.push(rbx);

        _data.Assembler.movdqu(__[rsp - 16 * 1], xmm0);
        _data.Assembler.movdqu(__[rsp - 16 * 2], xmm1);
        _data.Assembler.movdqu(__[rsp - 16 * 3], xmm2);
        _data.Assembler.movdqu(__[rsp - 16 * 4], xmm3);
        _data.Assembler.movdqu(__[rsp - 16 * 5], xmm4);
        _data.Assembler.movdqu(__[rsp - 16 * 6], xmm5);
        _data.Assembler.movdqu(__[rsp - 16 * 7], xmm6);
        _data.Assembler.movdqu(__[rsp - 16 * 8], xmm7);
        _data.Assembler.movdqu(__[rsp - 16 * 9], xmm8);
        _data.Assembler.movdqu(__[rsp - 16 * 10], xmm9);
        _data.Assembler.movdqu(__[rsp - 16 * 11], xmm10);
        _data.Assembler.movdqu(__[rsp - 16 * 12], xmm11);
        _data.Assembler.movdqu(__[rsp - 16 * 13], xmm12);
        _data.Assembler.movdqu(__[rsp - 16 * 14], xmm13);
        _data.Assembler.movdqu(__[rsp - 16 * 15], xmm14);
        _data.Assembler.movdqu(__[rsp - 16 * 16], xmm15);
        _data.Assembler.sub(rsp, 16 * 16 + 8);
        _data.Assembler.mov(rbp, rsp);

        _data.Assembler.call(_data.Labels["main"].LabelByRef);

        _data.Assembler.mov(rsp, rbp);
        _data.Assembler.add(rsp, 16 * 16 + 8);
        _data.Assembler.movdqu(xmm15, __[rsp - 16 * 16]);
        _data.Assembler.movdqu(xmm14, __[rsp - 16 * 15]);
        _data.Assembler.movdqu(xmm13, __[rsp - 16 * 14]);
        _data.Assembler.movdqu(xmm12, __[rsp - 16 * 13]);
        _data.Assembler.movdqu(xmm11, __[rsp - 16 * 12]);
        _data.Assembler.movdqu(xmm10, __[rsp - 16 * 11]);
        _data.Assembler.movdqu(xmm9, __[rsp - 16 * 10]);
        _data.Assembler.movdqu(xmm8, __[rsp - 16 * 9]);
        _data.Assembler.movdqu(xmm7, __[rsp - 16 * 8]);
        _data.Assembler.movdqu(xmm6, __[rsp - 16 * 7]);
        _data.Assembler.movdqu(xmm5, __[rsp - 16 * 6]);
        _data.Assembler.movdqu(xmm4, __[rsp - 16 * 5]);
        _data.Assembler.movdqu(xmm3, __[rsp - 16 * 4]);
        _data.Assembler.movdqu(xmm2, __[rsp - 16 * 3]);
        _data.Assembler.movdqu(xmm1, __[rsp - 16 * 2]);
        _data.Assembler.movdqu(xmm0, __[rsp - 16 * 1]);

        _data.Assembler.pop(rbx);
        _data.Assembler.pop(r9);
        _data.Assembler.pop(r8);
        _data.Assembler.pop(rcx);
        _data.Assembler.pop(rdx);
        _data.Assembler.pop(rsi);
        _data.Assembler.pop(rdi);

        _data.Assembler.pop(r11);
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