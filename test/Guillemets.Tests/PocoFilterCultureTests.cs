using Shouldly;

namespace Guillemets.Tests;

[SetCulture("de-DE")]
public class PocoFilterCultureTests
{
    [Test]
    public void Currency_filter_round_trips_correctly_under_ambient_culture_on_poco_decimal()
    {
        var actual = Template.Create("«amount / currency»").RenderObject(new { Amount = 1234.5m });

        actual.ShouldBe("1.234,50 €");
    }

    [Test]
    public void Date_filter_round_trips_correctly_under_ambient_culture_on_poco_datetime()
    {
        var actual = Template.Create("«date / date: dd/MM/yyyy»").RenderObject(new { Date = new DateTime(2026, 7, 11) });

        actual.ShouldBe("11.07.2026");
    }
}