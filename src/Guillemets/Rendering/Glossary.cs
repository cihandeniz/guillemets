using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;

namespace Guillemets.Rendering;

internal class Glossary
{
    static readonly ConcurrentDictionary<(IStringLocalizer? Localizer, string Culture), Glossary> CACHE = new();

    public static Glossary GetOrCreate(IStringLocalizer? localizer) =>
        CACHE.GetOrAdd((localizer, CultureInfo.CurrentUICulture.Name), static key => new(key.Localizer));

    readonly Dictionary<string, string>? _entries;

    Glossary(IStringLocalizer? localizer) =>
        _entries = localizer
            ?.GetAllStrings(includeParentCultures: true)
            .ToDictionary(entry => entry.Value, entry => entry.Name, StringComparer.OrdinalIgnoreCase);

    public string this[string segment] =>
        _entries is not null && _entries.TryGetValue(segment, out var mapped) ? mapped : segment.Dehumanize();
}