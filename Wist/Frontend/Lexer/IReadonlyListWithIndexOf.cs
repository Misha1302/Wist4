namespace Wist.Frontend.Lexer;

public interface IReadonlyListWithIndexOf<T> : IReadOnlyList<T>
{
    int IndexOf(T key);
}