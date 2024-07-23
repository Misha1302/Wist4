namespace Wist;

using Wist.Logger;

public class SourceCodeReader(ILogger logger)
{
    public string Read(string path)
    {
        var source = File.ReadAllText(path);
        logger.Log(source);
        return source;
    }
}