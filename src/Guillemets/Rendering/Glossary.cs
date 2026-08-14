using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;

namespace Guillemets.Rendering;

internal class Glossary
{
    static readonly ConcurrentDictionary<(
        IStringLocalizer? Localizer,
        string Culture,
        Func<string, string> PropertyNameConversion,
        Func<IEnumerable<string>, string>? CollisionResolver
    ), Glossary> CACHE = new();

    public static Glossary GetOrCreate(
        IStringLocalizer? localizer,
        Func<string, string> propertyNameConversion,
        Func<IEnumerable<string>, string>? collisionResolver
    ) =>
        CACHE.GetOrAdd(
            (localizer, CultureInfo.CurrentUICulture.Name, propertyNameConversion, collisionResolver),
            static key => new(key.Localizer, key.PropertyNameConversion, key.CollisionResolver)
        );

    static Dictionary<string, string> BuildEntries(
        IStringLocalizer localizer,
        Func<string, string> propertyNameConversion,
        Func<IEnumerable<string>, string>? collisionResolver
    ) =>
        localizer.GetAllStrings(includeParentCultures: true)
            .GroupBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => propertyNameConversion(ResolveName(group, collisionResolver)), StringComparer.OrdinalIgnoreCase);

    static string ResolveName(IGrouping<string, LocalizedString> group, Func<IEnumerable<string>, string>? collisionResolver)
    {
        var names = group.Select(entry => entry.Name).ToList();
        if (names.Count == 1) { return names[0]; }
        if (collisionResolver is not null) { return collisionResolver(names); }

        var sortedNames = names.OrderBy(name => name, StringComparer.Ordinal);

        throw new GlossaryException(
            $"Glossary has multiple entries that translate to '{group.Key}': {string.Join(" and ", sortedNames.Select(name => $"'{name}'"))}."
        );
    }

    readonly Dictionary<string, string>? _entries;
    readonly Func<string, string> _propertyNameConversion;
    readonly ConcurrentDictionary<string, string> _conversionCache = new();

    Glossary(IStringLocalizer? localizer, Func<string, string> propertyNameConversion, Func<IEnumerable<string>, string>? collisionResolver)
    {
        _propertyNameConversion = propertyNameConversion;
        _entries = localizer is null ? null : BuildEntries(localizer, propertyNameConversion, collisionResolver);
    }

    public string this[string segment] =>
        _entries is not null && _entries.TryGetValue(segment, out var mapped)
            ? mapped
            : _conversionCache.GetOrAdd(segment, _propertyNameConversion);
}