namespace Wist.Backend.Compiler;

public class MetaData
{
    private readonly List<FunctionMetaData> _functions = [];
    public IReadOnlyList<FunctionMetaData> Functions => _functions;

    public void AddFunction(FunctionMetaData functionMetaData)
    {
        _functions.Add(functionMetaData);
    }
}