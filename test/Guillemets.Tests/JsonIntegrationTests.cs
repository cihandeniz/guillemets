using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class JsonIntegrationTests
{
    [Test]
    public void Renders_simple_property_from_json()
    {
        var data = JsonDocument.Parse("""{"Name": "Alice"}""").RootElement;

        var actual = Template.Create("Hello «name»!").Render(data);

        actual.ShouldBe("Hello Alice!");
    }

    [Test]
    public void Render_produces_customer_offer_integration_output()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.guil.md");
        var jsonPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.json");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.md");

        var template = Template.Create(File.ReadAllText(guilPath));
        var data = JsonDocument.Parse(File.ReadAllText(jsonPath)).RootElement;

        template.Render(data).ShouldBe(File.ReadAllText(expectedPath));
    }

    [Test]
    public void Render_produces_almost_errors_integration_output()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.guil.md");
        var jsonPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.json");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.md");

        var template = Template.Create(File.ReadAllText(guilPath));
        var data = JsonDocument.Parse(File.ReadAllText(jsonPath)).RootElement;

        template.Render(data).ShouldBe(File.ReadAllText(expectedPath));
    }
}