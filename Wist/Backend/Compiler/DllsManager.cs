namespace Wist.Backend.Compiler;

using System.Reflection;
using InfoAboutMethod = (nint ptr, System.Reflection.ParameterInfo[] parameters, Type returnType);

public class DllsManager
{
    private readonly Dictionary<string, InfoAboutMethod> _functions = [];

    public void Import(string path)
    {
        var functions = Assembly.LoadFrom(path).GetTypes()
            .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static));

        foreach (var function in functions)
            _functions.Add(function.Name, (function.MethodHandle.GetFunctionPointer(), function.GetParameters(), function.ReturnType));
    }

    public InfoAboutMethod GetPointerOf(string functionName) => _functions[functionName];
}