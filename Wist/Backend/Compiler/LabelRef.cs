namespace Wist.Backend.Compiler;

using Iced.Intel;

public class LabelRef(Label label)
{
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private Label _label = label;

    public ref Label LabelByRef => ref _label;
}