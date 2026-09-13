using Shouldly;

namespace Guillemets.Tests;

public class PocoIntegrationTests
{
    [Test]
    public void Renders_simple_property_from_poco()
    {
        var actual = Template.Create("Hello «name»!").RenderObject(new { Name = "Alice" });

        actual.ShouldBe("Hello Alice!");
    }

    [Test]
    public void Render_produces_customer_offer_integration_output()
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

    [Test]
    public void Render_produces_almost_errors_integration_output()
    {
        var guilPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.guil.md");
        var expectedPath = Path.Combine(SpecsRoot.PATH, "09-integration", "002-almost-errors.md");

        var data = new
        {
            Author = "Alice",
            Reviewer = (object?)null,
            Reviewers = Array.Empty<object>(),
            Approvers = new[]
            {
                new { Name = "Bob", Approved = false },
                new { Name = "Carol", Approved = false },
            },
            SingleTag = new[] { "urgent" },
            NoTags = Array.Empty<string>(),
            Tags = new[] { "philosophy", "wisdom" },
        };

        var template = Template.Create(File.ReadAllText(guilPath));

        template.RenderObject(data).ShouldBe(File.ReadAllText(expectedPath));
    }
}