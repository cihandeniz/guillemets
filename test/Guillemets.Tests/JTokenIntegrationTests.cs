using Newtonsoft.Json.Linq;
using Shouldly;

namespace Guillemets.Tests;

public class JTokenIntegrationTests
{
    [Test]
    public void RendersSimplePropertyFromJToken()
    {
        var data = new JObject { ["Name"] = "Alice" };

        var actual = Template.Create("Hello «name»!").Render(data);

        actual.ShouldBe("Hello Alice!");
    }

    [Test]
    public void Render_ProducesCustomerOfferIntegrationOutput()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.guil.md");
        var jsonPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.json");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.md");

        var template = Template.Create(File.ReadAllText(guilPath));
        var data = JToken.Parse(File.ReadAllText(jsonPath));

        template.Render(data).ShouldBe(File.ReadAllText(expectedPath));
    }

    [Test]
    [Ignore("depends on a block header naming a truly-missing property resolving to falsy instead of throwing — see PropertyResolver.Project, not yet implemented")]
    public void Render_ProducesAlmostErrorsIntegrationOutput()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.guil.md");
        var jsonPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.json");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.md");

        var template = Template.Create(File.ReadAllText(guilPath));
        var data = JToken.Parse(File.ReadAllText(jsonPath));

        template.Render(data).ShouldBe(File.ReadAllText(expectedPath));
    }
}