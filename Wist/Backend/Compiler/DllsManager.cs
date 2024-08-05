using System.Reflection;

namespace Wist.Backend.Compiler;

using InfoAboutMethod = (nint ptr, ParameterInfo[] parameters, Type returnType);

public class DllsManager
{
    private readonly Dictionary<string, InfoAboutMethod> _functions = [];

    public void Import(string path)
    {
        var functions = Assembly.LoadFrom(path)
            .GetTypes()
            .Where(x => x.Name.EndsWith("Library"))
            .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .ToList();

        foreach (var function in functions)
            _functions.Add(function.Name,
                (function.MethodHandle.GetFunctionPointer(), function.GetParameters(), function.ReturnType));
    }

    public InfoAboutMethod GetPointerOf(string functionName)
    {
        return _functions[functionName];
    }

    public bool HasFunction(string funcName)
    {
        return _functions.ContainsKey(funcName);
    }
}