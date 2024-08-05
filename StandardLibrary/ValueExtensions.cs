namespace StandardLibrary;

public static class ValueExtensions
{
    public static unsafe TTo As<TFrom, TTo>(this TFrom value) where TFrom : unmanaged where TTo : unmanaged
    {
        return *(TTo*)&value;
    }
}