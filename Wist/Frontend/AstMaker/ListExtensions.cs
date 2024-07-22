namespace Wist.Frontend.AstMaker;

using System.Diagnostics;

public static class ListExtensions
{
    public static void AddAndRemove(this AstNode dest, List<AstNode> source, params int[] indices)
    {
        Debug.Assert(indices.SequenceEqual(indices.Order()));

        var children = indices.Select(index => source[index]).ToList();
        dest.Children.AddRange(children);
        children.SetParent(dest);

        for (var i = indices.Length - 1; i >= 0; i--)
            source.RemoveAt(indices[i]);
    }

    public static void SetParent(this List<AstNode> list, AstNode parent)
    {
        foreach (var element in list)
            element.Parent = parent;
    }
}