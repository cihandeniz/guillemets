using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

[SetCulture("tr-TR")]
public class FilterFormattingTests
{
    [Test]
    public void Date_filter_formats_with_given_pattern()
    {
        var data = JsonDocument.Parse("""{"Date": "2026-07-11"}""").RootElement;

        var actual = Template.Create("Date: «date / date: dd/MM/yyyy»").Render(data);

        actual.ShouldBe("Date: 11.07.2026");
    }

    [Test]
    public void Currency_filter_with_no_argument_uses_ambient_culture_symbol_and_default_decimals()
    {
        var data = JsonDocument.Parse("""{"Amount": 1500}""").RootElement;

        var actual = Template.Create("Amount: «amount / currency»").Render(data);

        actual.ShouldBe("Amount: ₺1.500,00");
    }

    [Test]
    public void Currency_filter_argument_overrides_decimal_count_keeping_ambient_symbol()
    {
        var data = JsonDocument.Parse("""{"Amount": 1500}""").RootElement;

        var actual = Template.Create("Amount: «amount / currency: C0»").Render(data);

        actual.ShouldBe("Amount: ₺1.500");
    }

    [Test]
    public void Number_filter_formats_with_given_pattern()
    {
        var data = JsonDocument.Parse("""{"Amount": 1234.5}""").RootElement;

        var actual = Template.Create("Amount: «amount / number: N2»").Render(data);

        actual.ShouldBe("Amount: 1.234,50");
    }

    [Test]
    public void Number_filter_with_no_argument_uses_ambient_culture_default_formatting()
    {
        var data = JsonDocument.Parse("""{"Amount": 1234.5}""").RootElement;

        var actual = Template.Create("Amount: «amount / number»").Render(data);

        actual.ShouldBe("Amount: 1234,5");
    }

    [Test]
    public void Truncate_filter_does_not_split_a_surrogate_pair()
    {
        var data = JsonDocument.Parse("""{"Description": "AB😀CD"}""").RootElement;

        var actual = Template.Create("«description / truncate: 3»").Render(data);

        actual.ShouldBe("AB…");
    }
}