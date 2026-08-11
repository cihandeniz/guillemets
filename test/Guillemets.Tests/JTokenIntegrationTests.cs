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
    [Ignore("depends on the tables and filters fixture groups, not yet implemented")]
    public void Render_ProducesCustomerOfferIntegrationOutput()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.guil.md");
        var jsonPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.json");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.md");

        var template = Template.Create(File.ReadAllText(guilPath));
        var data = JToken.Parse(File.ReadAllText(jsonPath));

        template.Render(data).ShouldBe(File.ReadAllText(expectedPath));
    }
}