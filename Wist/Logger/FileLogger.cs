namespace Wist.Logger;

public class FileLogger : ILogger
{
    public FileLogger()
    {
        File.WriteAllText("logs.txt", "");
    }

    public void Log(string msg, string filePath = "", int lineNumber = 0)
    {
        File.AppendAllText("logs.txt", MakeLogMessage());

        string MakeLogMessage()
        {
            var prefix = $"path: '{filePath}'; line: {lineNumber}; ";
            if (msg.Contains('\n'))
                return prefix + $"\n{msg}";
            return prefix + msg;
        }
    }
}