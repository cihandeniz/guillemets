using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Filters;

/// <summary>
/// The filters a <see cref="Template"/> resolves <c>«expr / filter: arg»</c>
/// stages against — reachable via <see cref="ParseOptions.Filters"/>.
/// </summary>
public class FilterRegistry
{
    const string FILTER_SUFFIX = "Filter";

    /// <summary>A registry with every built-in filter registered.</summary>
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

    /// <summary>
    /// Registers <paramref name="filter"/> under a template name derived
    /// from its type — a trailing <c>Filter</c> suffix is stripped if
    /// present, and the rest is lower-cased/word-split the same way
    /// property names are (<c>ReverseFilter</c> → <c>reverse</c>,
    /// <c>SmartQuotes</c> → <c>smart quotes</c>). Re-registering an
    /// existing name replaces it — this is how a built-in like
    /// <see cref="CurrencyFilter"/> gets swapped for a differently
    /// configured instance.
    /// </summary>
    /// <param name="filter">The filter instance to register.</param>
    /// <returns>This registry, for chaining.</returns>
    public FilterRegistry Register<TFilter>(TFilter filter)
        where TFilter : IFilter
    {
        _filters[NameFor<TFilter>()] = filter;

        return this;
    }

    /// <summary>
    /// Drops a filter entirely, built-in or custom, making its name
    /// unavailable.
    /// </summary>
    /// <returns>This registry, for chaining.</returns>
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