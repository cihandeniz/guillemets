using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

internal class FilterRegistry
{
    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register(string name, IFilter filter)
    {
        _filters[name] = filter;

        return this;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}