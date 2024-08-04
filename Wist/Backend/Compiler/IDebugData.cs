namespace Wist.Backend.Compiler;

public interface IDebugData
{
    public void Add(int instructionIndex, int depthLevel, string message);
    public bool TryGet(int instructionIndex, out List<(int deepthLevel, string message)> value);
}