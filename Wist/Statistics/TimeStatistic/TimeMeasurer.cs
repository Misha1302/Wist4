using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wist.Statistics.Logger;

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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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