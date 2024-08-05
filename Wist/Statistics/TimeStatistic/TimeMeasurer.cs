using System.Diagnostics;
using Wist.Logger;

namespace Wist.Statistics.TimeStatistic;

public class TimeMeasurer(ILogger logger)
{
    private string _methodName = null!;
    private Stopwatch _sw = null!;

    public void Start(string methodName)
    {
        _sw = Stopwatch.StartNew();
        _methodName = methodName;
    }

    public void End()
    {
        _sw.Stop();
        logger.Log($"{_methodName} was executed in {_sw.ElapsedMilliseconds} ms");
    }

    public void Measure(Action action)
    {
        Start(action.Method.Name);
        action();
        End();
    }
}