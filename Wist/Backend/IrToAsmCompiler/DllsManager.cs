using System.Reflection;

namespace Wist.Backend.IrToAsmCompiler;

using InfoAboutMethod = (nint ptr, ParameterInfo[] parameters, Type returnType);

public class DllsManager
{
    private readonly Dictionary<string, InfoAboutMethod> _functions = [];
    private readonly List<string> _importsPaths = [];
    public IReadOnlyList<string> ImportsPaths => _importsPaths;

    public void Import(string path)
    {
        if (_importsPaths.Contains(path)) return;
        _importsPaths.Add(path);

        var functions = Assembly.LoadFrom(path)
            .GetTypes()
            .Where(x => x.Name.EndsWith("Library") || x.Name.EndsWith("Lib"))
            .Select(x =>
            {
                var fieldInfo = x.GetField("Prefix");
                return (type: x, prefix: fieldInfo?.GetValue(null) ?? "");
            })
            .Select(x =>
                (x.prefix, methods: x.type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            );

        foreach (var function in functions)
        foreach (var method in function.methods)
            _functions.Add(
                function.prefix + method.Name,
                (method.MethodHandle.GetFunctionPointer(), method.GetParameters(), method.ReturnType)
            );
    }

    public InfoAboutMethod GetPointerOf(string functionName)
    {
        return _functions[functionName];
    }

    public bool HaveFunction(string funcName)
    {
        return _functions.ContainsKey(funcName);
    }
}