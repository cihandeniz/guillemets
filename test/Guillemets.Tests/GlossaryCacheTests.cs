using Microsoft.Extensions.Localization;
using Shouldly;
using System.Globalization;
using System.Text.Json;

namespace Guillemets.Tests;

public class GlossaryCacheTests
{
    [Test]
    public void Glossary_cache_is_keyed_by_culture_not_shared_across_cultures()
    {
        var localizer = new CultureAwareLocalizer();
        var data = JsonDocument.Parse("""{"OfferNo": "2026-0711"}""").RootElement;

        RenderUnder("en-US", "«quote no»", localizer, data).ShouldBe("2026-0711");
        RenderUnder("tr-TR", "«teklif no»", localizer, data).ShouldBe("2026-0711");
        RenderUnder("en-US", "«quote no»", localizer, data).ShouldBe("2026-0711");
    }

    static string RenderUnder(string culture, string template, IStringLocalizer localizer, JsonElement data)
    {
        var original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            return Template.Create(template, options => options.Glossary = localizer).Render(data);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    class CultureAwareLocalizer : IStringLocalizer
    {
        static readonly Dictionary<string, string> EN = new() { ["OfferNo"] = "Quote No" };
        static readonly Dictionary<string, string> TR = new() { ["OfferNo"] = "Teklif No" };

        static Dictionary<string, string> Entries =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? TR : EN;

        public LocalizedString this[string name] =>
            Entries.TryGetValue(name, out var value) ? new(name, value) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Entries.Select(entry => new LocalizedString(entry.Key, entry.Value));
    }
}