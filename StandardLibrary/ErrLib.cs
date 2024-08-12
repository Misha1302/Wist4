namespace StandardLibrary;

public static class ErrLib
{
    public static readonly string Prefix = "Err::";

    public static void Throw(long message)
    {
        throw new AssemblerInnerException(StringOperationsLib.ArrToStr(message));
    }

    public class AssemblerInnerException(string arrToStr) : Exception(arrToStr);
}