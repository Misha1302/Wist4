using Wist.Backend.IrToAsmCompiler.TypeSystem;

namespace Wist.Backend.AstToIrCompiler;

/// <summary>
///     This type is mostly need to structures. If you have local 'vec' and it's type is Vector3,
///     then you can create real 'vec.x', 'vec.y', 'vec.z' and alias 'vec' with alias='vec:x', 'cause
///     it's reference to first element (it's need to add possibility to add to reference some offset to get any field of
///     structure).
///     AliasType will be 'Vector3' in this case, 'cause we need to save source type of local
/// </summary>
/// <param name="Alias">name of the local being referenced</param>
/// <param name="RealLocalsInfo">locals that references this alias</param>
/// <param name="AliasType">source type of this local</param>
public record IrLocalAlias(string Alias, List<IIrLocalInfo> RealLocalsInfo, string AliasType) : IIrLocalInfo
{
    public string RealName => RealLocalsInfo[0].Name;
    public string Name => Alias;
    public AsmValueType Type => RealLocalsInfo[0].Type;
}