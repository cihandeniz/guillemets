using Guillemets.Data;
using Guillemets.Data.Json;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class JsonElementDataSourceTests : DataSourceSpec
{
    static JsonElementDataSource Parse(string json) =>
        new(JsonDocument.Parse(json).RootElement);

    protected override IDataSource CreateObjectWithFullName(string value) =>
        Parse($$"""{"fullName": "{{value}}"}""");

    protected override IDataSource CreateScalar(string value) =>
        Parse($"\"{value}\"");

    [Test]
    public void Kind_returns_object_for_json_object() =>
        Parse("{}").Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_returns_array_for_json_array() =>
        Parse("[]").Kind.ShouldBe(DataKind.Array);

    [Test]
    public void Kind_returns_string_for_json_string() =>
        Parse("\"Alice\"").Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_number_for_json_number() =>
        Parse("42").Kind.ShouldBe(DataKind.Number);

    [TestCase("true")]
    [TestCase("false")]
    public void Kind_returns_boolean_for_json_true_or_false(string json) =>
        Parse(json).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_returns_null_for_json_null() =>
        Parse("null").Kind.ShouldBe(DataKind.Null);

    [Test]
    public void Try_get_property_returns_wrapped_value_when_property_exists()
    {
        var source = Parse("""{"Name": "Alice"}""");

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void Enumerate_array_returns_wrapped_items()
    {
        var items = Parse("""["a", "b"]""").EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase("true", true)]
    [TestCase("false", false)]
    public void As_boolean_returns_underlying_value(string json, bool expected) =>
        Parse(json).AsBoolean().ShouldBe(expected);

    [Test]
    public void As_boolean_returns_false_for_null() =>
        Parse("null").AsBoolean().ShouldBeFalse();

    [Test]
    public void As_display_string_returns_underlying_value_text()
    {
        Parse("\"Alice\"").AsDisplayString().ShouldBe("Alice");
        Parse("42").AsDisplayString().ShouldBe("42");
    }
}