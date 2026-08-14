using Guillemets.Filters;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

[SetCulture("en-US")]
public class FilterRegistryTests
{
    [Test]
    public void Register_with_an_instance_overrides_the_default_currency_symbol()
    {
        var data = JsonDocument.Parse("""{"Amount": 1234.5}""").RootElement;

        var actual = Template.Create("«amount / currency»", options =>
            options.Filters.Register(new CurrencyFilter("TL"))
        ).Render(data);

        actual.ShouldBe("TL1,234.50");
    }

    [Test]
    public void Remove_makes_a_built_in_filter_unavailable()
    {
        var data = JsonDocument.Parse("""{"Description": "hello"}""").RootElement;

        var exception = Should.Throw<TemplateParseException>(() =>
            Template.Create("«description / truncate: 3»", options => options.Filters.Remove<TruncateFilter>()).Render(data)
        );

        exception.Message.ShouldStartWith("Unknown filter 'truncate'");
    }
}