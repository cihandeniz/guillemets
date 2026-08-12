namespace Guillemets.Filters;

internal class JoinLastFilter : IFilter
{
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