using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

public class FilterRegistry
{
    public static FilterRegistry CreateDefault(Action<FilterRegistry> configureFilters)
    {
        var result = new FilterRegistry()
            .Register("join", new JoinFilter())
            .Register("date", new DateFilter());
        configureFilters(result);

        return result;
    }

    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register(string name, IFilter filter)
    {
        _filters[name] = filter;

        return this;
    }

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}