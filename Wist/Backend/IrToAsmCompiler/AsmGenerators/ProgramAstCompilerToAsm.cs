using Iced.Intel;
using Wist.Backend.AstToIrCompiler;
using Wist.Backend.Executing;
using Wist.Statistics.Logger;

namespace Wist.Backend.IrToAsmCompiler.AsmGenerators;

public class ProgramAstCompilerToAsm(ILogger logger) : IAstCompiler
{
    private AstCompilerData _data = null!;

    public IExecutable Compile(IrImage image)
    {
        Init(image);

        EmitFunctions(image.Functions);
        EmitStartPoint();
        EmitStaticData(image.StaticData);
        EmitFunctionCodes(image.Functions);
        return GetExecutable();
    }

    private void EmitStaticData(Dictionary<string, byte[]> staticData)
    {
        foreach (var pair in staticData)
        {
            var labelRef = new LabelRef(_data.Assembler.CreateLabel(pair.Key));
            _data.Labels.Add(pair.Key, labelRef);

            _data.Assembler.Label(ref labelRef.LabelByRef);
            _data.Assembler.db(pair.Value);
        }
    }

    private void Init(IrImage image)
    {
        var assembler = new Assembler(64);
        _data = new AstCompilerData(
            assembler, [], new CompilerHelper(),
            new DebugData.DebugData(), new StackManager(assembler), image
        );
    }

    private IExecutable GetExecutable()
    {
        return OS.IsLinux()
            ? new LinuxAsmExecutable(_data.Assembler, _data.DebugData, logger)
            : OS.IsWindows()
                ? new WindowsAsmExecutable(_data.Assembler, _data.DebugData, logger)
                : throw new InvalidOperationException("No supported executable for this OS");
    }

    private void EmitFunctionCodes(List<IrFunction> functions)
    {
        foreach (var function in functions)
            new IrFunctionCompilerToAsm(_data, function).Compile(function.Instructions);
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

    private void EmitFunctions(List<IrFunction> functions)
    {
        foreach (var func in functions)
            _data.Labels.Add(func.Name, new LabelRef(_data.Assembler.CreateLabel(func.Name)));
    }
}