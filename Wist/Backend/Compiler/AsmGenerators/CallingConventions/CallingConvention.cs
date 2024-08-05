using Iced.Intel;

namespace Wist.Backend.Compiler.AsmGenerators.CallingConventions;

public record CallingConvention(List<(AssemblerRegister64 i64, AssemblerRegisterXMM f64)> ArgumentRegisters);