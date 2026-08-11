using Shouldly;

namespace Guillemets.Tests;

public class PocoIntegrationTests
{
    [Test]
    public void RendersSimplePropertyFromPoco()
    {
        var actual = Template.Create("Hello «name»!").RenderObject(new { Name = "Alice" });

        actual.ShouldBe("Hello Alice!");
    }

    [Test]
    [Ignore("depends on the tables and filters fixture groups, not yet implemented")]
    public void Render_ProducesCustomerOfferIntegrationOutput()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.guil.md");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "001-customer-offer.md");

        var data = new
        {
            QuoteNo = "2026-0711",
            Individual = false,
            FullName = "Alice Smith",
            CompanyName = "Acme Consulting Inc.",
            Date = "2026-07-11",
            ValidUntil = "2026-08-11",
            Items = new[]
            {
                new { Description = "Consulting", Quantity = 2, Unit = "day", UnitPrice = 1500, Total = 3000 },
                new { Description = "Setup", Quantity = 1, Unit = "unit", UnitPrice = 500, Total = 500 },
            },
            Subtotal = 3500,
            TaxRate = 20,
            Tax = 700,
            GrandTotal = 4200,
            Company = "Acme Consulting Inc.",
        };

        var template = Template.Create(File.ReadAllText(guilPath));

        template.RenderObject(data).ShouldBe(File.ReadAllText(expectedPath));
    }
}