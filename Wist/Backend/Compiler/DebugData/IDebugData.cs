namespace Wist.Backend.Compiler.DebugData;

public interface IDebugData
{
    public void Add(int instructionIndex, int depthLevel, string message);
    public bool TryGet(int instructionIndex, out List<(int depthLevel, string message)> value);
}