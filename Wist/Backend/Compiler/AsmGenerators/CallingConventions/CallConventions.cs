namespace Wist.Backend.Compiler.AsmGenerators.CallingConventions;

public static class CallConventions
{
    // System V AMD64 ABI
    // https://en.wikipedia.org/wiki/X86_calling_conventions
    public static readonly CallingConvention SystemVAmd64Abi = new(
        [(rdi, xmm0), (rsi, xmm1), (rdx, xmm2), (rcx, xmm3), (r8, xmm4), (r9, xmm5)]
    );

    // Microsoft x64 calling convention
    // https://en.wikipedia.org/wiki/X86_calling_conventions
    public static readonly CallingConvention MicrosoftX64 = new(
        [(rcx, xmm0), (rdx, xmm1), (r8, xmm2), (r9, xmm3)]
    );
}