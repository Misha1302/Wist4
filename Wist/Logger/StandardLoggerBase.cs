namespace Wist.Logger;

using System.Diagnostics;

public abstract class StandardLoggerBase(Action<string> logMethod) : ILogger
{
    public void Log(string msg)
    {
        logMethod(MakeLogMessage(msg));
    }

    private static string MakeLogMessage(string msg)
    {
        var callStack = new StackTrace().GetFrames().Select(x => x.GetMethod()!.Name);
        var prefix = $"call stack: '{string.Join("->", callStack.Reverse().SkipLast(2))}'; msg: \n";
        const string postfix = "\n--------------------------------------------------------\n";
        return prefix + msg + postfix;
    }
}