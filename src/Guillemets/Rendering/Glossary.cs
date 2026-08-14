using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;

namespace Guillemets.Rendering;

internal class Glossary
{
    static readonly ConcurrentDictionary<(IStringLocalizer? Localizer, string Culture, Func<string, string> PropertyNameConversion), Glossary> CACHE = new();

    public static Glossary GetOrCreate(IStringLocalizer? localizer, Func<string, string> propertyNameConversion) =>
        CACHE.GetOrAdd((localizer, CultureInfo.CurrentUICulture.Name, propertyNameConversion), static key => new(key.Localizer, key.PropertyNameConversion));

    readonly Dictionary<string, string>? _entries;
    readonly Func<string, string> _propertyNameConversion;

    Glossary(IStringLocalizer? localizer, Func<string, string> propertyNameConversion)
    {
        _propertyNameConversion = propertyNameConversion;
        _entries = localizer
            ?.GetAllStrings(includeParentCultures: true)
            .ToDictionary(entry => entry.Value, entry => propertyNameConversion(entry.Name), StringComparer.OrdinalIgnoreCase);
    }

    public string this[string segment] =>
        _entries is not null && _entries.TryGetValue(segment, out var mapped) ? mapped : _propertyNameConversion(segment);
}