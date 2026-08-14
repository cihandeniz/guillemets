using Guillemets.Tests.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class GlossaryResourceIntegrationTests
{
    static IStringLocalizer CreateLocalizer() =>
        new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance)
            .Create(typeof(Glossary));

    [Test]
    public void Resx_backed_glossary_resolves_mapped_term()
    {
        var data = JsonDocument.Parse("""{"OfferNo": "2026-0711"}""").RootElement;

        var actual = Template.Create("Quote No: «quote no»", options => options.Localizer = CreateLocalizer()).Render(data);

        actual.ShouldBe("Quote No: 2026-0711");
    }

    [Test]
    public void Resx_backed_glossary_falls_back_to_direct_resolution_for_unmapped_term()
    {
        var data = JsonDocument.Parse("""{"FullName": "Alice Smith"}""").RootElement;

        var actual = Template.Create("«full name»", options => options.Localizer = CreateLocalizer()).Render(data);

        actual.ShouldBe("Alice Smith");
    }
}