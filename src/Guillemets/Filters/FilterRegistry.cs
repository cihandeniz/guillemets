using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

public class FilterRegistry
{
    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register(string name, IFilter filter)
    {
        _filters[name] = filter;

        return this;
    }

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}