namespace Wist.Statistics.Logger;

public interface ILogger
{
    public void Log(string msg, LogType logType = LogType.Info);
}