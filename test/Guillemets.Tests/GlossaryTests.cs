using Guillemets.Rendering;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class GlossaryTests
{
    [Test]
    public void Duplicate_glossary_translations_throw_a_clear_error()
    {
        var localizer = new FakeStringLocalizer(new Dictionary<string, string>
        {
            ["OfferNo"] = "Quote No",
            ["InvoiceNo"] = "Quote No",
        });
        var data = JsonDocument.Parse("{}").RootElement;

        var actual = () => Template.Create("«quote no»", options => options.Localizer = localizer).Render(data);

        actual.ShouldThrow<GlossaryException>().Message.ShouldBe("Glossary has multiple entries that translate to 'Quote No': 'InvoiceNo' and 'OfferNo'.");
    }

    [Test]
    public void Collision_resolver_picks_the_winning_entry_instead_of_throwing()
    {
        var localizer = new FakeStringLocalizer(new Dictionary<string, string>
        {
            ["OfferNo"] = "Quote No",
            ["InvoiceNo"] = "Quote No",
        });
        var data = JsonDocument.Parse("""{"OfferNo":"2026-0711"}""").RootElement;

        var actual = Template.Create("«quote no»", options =>
        {
            options.Localizer = localizer;
            options.GlossaryCollisionResolver = names => names.OrderByDescending(name => name, StringComparer.Ordinal).First();
        }).Render(data);

        actual.ShouldBe("2026-0711");
    }
}