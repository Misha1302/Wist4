using System.Diagnostics;

namespace Wist.Statistics.Logger;

public abstract class StandardLoggerBase(Action<string> logMethod, bool printWarningsAndErrorsToConsole = true)
    : ILogger
{
    public void Log(string msg, LogType logType = LogType.Info)
    {
        var logMessage = MakeLogMessage(msg, logType);
        if (logType is LogType.Warning or LogType.Error && printWarningsAndErrorsToConsole)
            Console.WriteLine(logMessage);

        logMethod(logMessage);
    }

    private static string MakeLogMessage(string msg, LogType logType = LogType.Info)
    {
        var callStack = new StackTrace().GetFrames().Select(x => x.GetMethod()!.Name);
        var preprefix = logType switch
        {
            LogType.Info => "info: ",
            LogType.Warning => "WARNING: ",
            _ => "ERROR: ",
        };
        var prefix = $"call stack: '{string.Join("->", callStack.Reverse().SkipLast(2))}'; msg: \n";
        const string postfix = "\n--------------------------------------------------------\n";
        return preprefix + prefix + msg + postfix;
    }
}