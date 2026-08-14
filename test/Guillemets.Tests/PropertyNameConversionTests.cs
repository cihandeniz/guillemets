using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class PropertyNameConversionTests
{
    [Test]
    public void Default_conversion_dehumanizes_a_spaced_resource_key_to_match_the_model()
    {
        var localizer = new FakeStringLocalizer(new Dictionary<string, string> { ["Full Name"] = "Tam Ad" });
        var data = JsonDocument.Parse("""{"FullName": "Alice Smith"}""").RootElement;

        var actual = Template.Create("«tam ad»", options => options.Localizer = localizer).Render(data);

        actual.ShouldBe("Alice Smith");
    }

    [Test]
    public void Custom_conversion_replaces_the_default_dehumanize_entirely()
    {
        var localizer = new FakeStringLocalizer(new Dictionary<string, string> { ["Full Name"] = "Tam Ad" });
        var data = JsonDocument.Parse("""{"FullName": "Alice Smith"}""").RootElement;

        var actual = Template.Create("«tam ad»", options =>
        {
            options.Localizer = localizer;
            options.PropertyNameConversion = key => key;
        }).Render(data);

        actual.ShouldBe(string.Empty);
    }

    [Test]
    public void Custom_conversion_bridges_a_spaced_resource_key_to_a_snake_case_model()
    {
        var localizer = new FakeStringLocalizer(new Dictionary<string, string> { ["Full Name"] = "Tam Ad" });
        var data = JsonDocument.Parse("""{"full_name": "Alice Smith"}""").RootElement;

        var actual = Template.Create("«tam ad»", options =>
        {
            options.Localizer = localizer;
            options.PropertyNameConversion = ToSnakeCase;
        }).Render(data);

        actual.ShouldBe("Alice Smith");
    }

    [Test]
    public void Custom_conversion_also_governs_unmatched_segments_via_direct_resolution()
    {
        var data = JsonDocument.Parse("""{"full_name": "Alice Smith"}""").RootElement;

        var actual = Template.Create("«full name»", options => options.PropertyNameConversion = ToSnakeCase).Render(data);

        actual.ShouldBe("Alice Smith");
    }

    static string ToSnakeCase(string key) =>
        string.Join("_", key.Split(' ').Select(word => word.ToLowerInvariant()));
}