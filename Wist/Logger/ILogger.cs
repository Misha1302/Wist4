namespace Wist.Logger;

using System.Runtime.CompilerServices;

public interface ILogger
{
    public void Log(string msg, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
}