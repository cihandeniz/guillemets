using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

public class FilterRegistry
{
    const string FILTER_SUFFIX = "Filter";

    public static FilterRegistry CreateDefault() =>
        new FilterRegistry()
            .Register<JoinFilter>()
            .Register<JoinLastFilter>()
            .Register<DefaultFilter>()
            .Register<UpperFilter>()
            .Register<LowerFilter>()
            .Register<DateFilter>()
            .Register(new CurrencyFilter())
            .Register<NumberFilter>()
            .Register<TruncateFilter>()
        ;

    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register<TFilter>()
        where TFilter : IFilter, new() =>
        Register(new TFilter());

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
        typeof(TFilter).Name[..^FILTER_SUFFIX.Length].ToLowerWords();

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}