using Humanizer;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

public class FilterRegistry
{
    const string FILTER_SUFFIX = "Filter";

    public static FilterRegistry CreateDefault(Action<FilterRegistry> configureFilters)
    {
        var result = new FilterRegistry()
            .Register<JoinFilter>()
            .Register<DateFilter>()
            .Register<CurrencyFilter>()
            .Register<TruncateFilter>()
            .Register<JoinLastFilter>();
        configureFilters(result);

        return result;
    }

    readonly Dictionary<string, IFilter> _filters = [];

    public FilterRegistry Register<TFilter>()
        where TFilter : IFilter, new()
    {
        _filters[NameFor<TFilter>()] = new TFilter();

        return this;
    }

    static string NameFor<TFilter>() =>
        typeof(TFilter).Name[..^FILTER_SUFFIX.Length].Humanize(LetterCasing.LowerCase);

    internal bool TryGet(string name, [NotNullWhen(true)] out IFilter? filter) =>
        _filters.TryGetValue(name, out filter);
}