namespace Wist.Backend.IrToAsmCompiler.DebugData;

public class DebugData : IDebugData
{
    private readonly Dictionary<int, List<(int depthLevel, string message)>> _data = [];

    public void Add(int instructionIndex, int depthLevel, string message)
    {
        _data.TryAdd(instructionIndex, []);
        _data[instructionIndex].Add((depthLevel, message));
    }

    public bool TryGet(int instructionIndex, out List<(int depthLevel, string message)> value)
    {
        var success = _data.TryGetValue(instructionIndex, out value!);
        return success;
    }
}