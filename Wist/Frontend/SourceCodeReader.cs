using Wist.Statistics.Logger;

namespace Wist.Frontend;

public class SourceCodeReader(ILogger logger)
{
    public string Read(string path)
    {
        var source = File.ReadAllText(path);
        logger.Log(source);
        return source;
    }
}