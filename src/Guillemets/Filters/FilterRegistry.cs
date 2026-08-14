using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

public class FilterRegistry
{
    const string FILTER_SUFFIX = "Filter";

    public static FilterRegistry CreateDefault() =>
        new FilterRegistry()
            .Register(new JoinFilter())
            .Register(new JoinLastFilter())
            .Register(new DefaultFilter())
            .Register(new UpperFilter())
            .Register(new LowerFilter())
            .Register(new DateFilter())
            .Register(new CurrencyFilter())
            .Register(new NumberFilter())
            .Register(new TruncateFilter())
        ;

    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register<TFilter>(TFilter filter)
        where TFilter : IFilter
    {
        _filters[NameFor<TFilter>()] = filter;

        return this;
    }

    public FilterRegistry Remove<T>() where T : IFilter
    {
        _filters.Remove(NameFor<T>());

        return this;
    }

    static string NameFor<TFilter>() =>
        StripFilterSuffix(typeof(TFilter).Name).ToLowerWords();

    static string StripFilterSuffix(string name) =>
        name.EndsWith(FILTER_SUFFIX, StringComparison.Ordinal) ? name[..^FILTER_SUFFIX.Length] : name;

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}