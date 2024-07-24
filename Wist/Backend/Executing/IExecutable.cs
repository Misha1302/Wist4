namespace Wist.Backend.Executing;

public interface IExecutable
{
    public long Execute();
    public byte[] ToBinary();
}