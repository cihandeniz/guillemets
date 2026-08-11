using System.Collections.ObjectModel;

namespace Guillemets.Rendering;

internal class PropertyChain(IList<string> properties,
    bool lastSegmentNegated = false
) : ReadOnlyCollection<string>(properties)
{
    public bool LastSegmentNegated { get; } = lastSegmentNegated;

    public PropertyChain WithoutLast() =>
        new([.. this.Take(Count - 1)]);

    public PropertyChain Tail() =>
        new([.. this.Skip(1)], LastSegmentNegated);
}