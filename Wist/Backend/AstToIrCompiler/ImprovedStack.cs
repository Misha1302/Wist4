using System.Diagnostics;

namespace Wist.Backend.AstToIrCompiler;

public class ImprovedStack<T> : Stack<T>
{
    private (T first, T second) PopWithoutAssert()
    {
        var second = Pop();
        return (second, Pop());
    }

    /// <summary>
    ///     method that helps binary operations. Pop two values, ensure their types are equals, push one value. 2 - in, 1 - out
    /// </summary>
    /// <returns></returns>
    public T Pop2AndPush1Same()
    {
        var pair = PopWithoutAssert();
        Debug.Assert(Equals(pair.first, pair.second));
        Push(pair.first);
        return pair.first;
    }

    public void Pop1(T plannedToPop)
    {
        Debug.Assert(Equals(Pop(), plannedToPop));
    }
}