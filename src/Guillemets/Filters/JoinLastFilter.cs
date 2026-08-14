namespace Guillemets.Filters;

/// <summary>
/// Same underlying <c>string.Join</c> as <see cref="JoinFilter"/>, applied
/// to just the last two items — chain with <see cref="JoinFilter"/> for a
/// natural "A, B and C" list.
/// </summary>
public class JoinLastFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg)
    {
        var list = values.ToList();
        if (list.Count < 2) { return list; }

        var merged = $"{list[^2]}{arg}{list[^1]}";
        list.RemoveRange(list.Count - 2, 2);
        list.Add(merged);

        return list;
    }
}