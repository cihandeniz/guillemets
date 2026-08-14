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
            .Register<CurrencyFilter>()
            .Register<TruncateFilter>()
        ;

    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register<TFilter>()
        where TFilter : IFilter, new()
    {
        _filters[NameFor<TFilter>()] = new TFilter();

        return this;
    }

    static string NameFor<TFilter>() =>
        typeof(TFilter).Name[..^FILTER_SUFFIX.Length].ToLowerWords();

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}