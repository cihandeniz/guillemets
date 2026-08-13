using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

[SetCulture("de-DE")]
public class FilterCultureTests
{
    [Test]
    public void Date_filter_ignores_ambient_culture()
    {
        var data = JsonDocument.Parse("""{"Date": "2026-07-11"}""").RootElement;

        var actual = Template.Create("«date | date: dd/MM/yyyy»").Render(data);

        actual.ShouldBe("11/07/2026");
    }

    [Test]
    public void Currency_filter_ignores_ambient_culture()
    {
        var data = JsonDocument.Parse("""{"Amount": "1234.5"}""").RootElement;

        var actual = Template.Create("«amount | currency: $»").Render(data);

        actual.ShouldBe("$1,234.50");
    }

    [Test]
    [SetCulture("tr-TR")]
    public void Upper_filter_respects_ambient_culture()
    {
        var data = JsonDocument.Parse("""{"Name": "istanbul"}""").RootElement;

        var actual = Template.Create("«name | upper»").Render(data);

        actual.ShouldBe("İSTANBUL");
    }

    [Test]
    [SetCulture("tr-TR")]
    public void Lower_filter_respects_ambient_culture()
    {
        var data = JsonDocument.Parse("""{"Name": "ISTANBUL"}""").RootElement;

        var actual = Template.Create("«name | lower»").Render(data);

        actual.ShouldBe("ıstanbul");
    }
}