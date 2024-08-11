namespace Wist.Statistics.Logger;

public class FileLogger : StandardLoggerBase
{
    public FileLogger(string filePathToLog = "logs.txt") : base(msg => File.AppendAllText(filePathToLog, msg))
    {
        File.WriteAllText(filePathToLog, "");
    }
}