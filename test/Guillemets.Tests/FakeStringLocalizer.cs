using Microsoft.Extensions.Localization;

namespace Guillemets.Tests;

internal class FakeStringLocalizer(IReadOnlyDictionary<string, string> entries)
    : IStringLocalizer
{
    public LocalizedString this[string name] =>
        entries.TryGetValue(name, out var value)
            ? new(name, value)
            : new(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] =>
        this[name];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        entries.Select(entry => new LocalizedString(entry.Key, entry.Value));
}