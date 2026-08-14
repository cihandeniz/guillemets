using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class LineEndingNormalizationTests
{
    [Test]
    public void Crlf_template_normalizes_lf_in_data_values_up_to_crlf()
    {
        var template = "Name: «name»\r\nDone.\r\n";
        var data = JsonDocument.Parse("""{"Name": "Line1\nLine2"}""").RootElement;

        var actual = Template.Create(template).Render(data);

        actual.ShouldBe("Name: Line1\r\nLine2\r\nDone.\r\n");
    }

    [Test]
    public void Crlf_template_does_not_double_up_data_values_that_already_use_crlf()
    {
        var template = "Name: «name»\r\nDone.\r\n";
        var data = JsonDocument.Parse("""{"Name": "Line1\r\nLine2"}""").RootElement;

        var actual = Template.Create(template).Render(data);

        actual.ShouldBe("Name: Line1\r\nLine2\r\nDone.\r\n");
    }

    [Test]
    public void Lf_template_normalizes_crlf_in_data_values_down_to_lf()
    {
        var template = "Name: «name»\nDone.\n";
        var data = JsonDocument.Parse("""{"Name": "Line1\r\nLine2"}""").RootElement;

        var actual = Template.Create(template).Render(data);

        actual.ShouldBe("Name: Line1\nLine2\nDone.\n");
    }
}