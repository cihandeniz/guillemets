using Guillemets.Data;
using Guillemets.Data.Json;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class JsonElementDataSourceTests
{
    [Test]
    public void Kind_ReturnsObject_ForJsonObject() =>
        Parse("{}").Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_ReturnsArray_ForJsonArray() =>
        Parse("[]").Kind.ShouldBe(DataKind.Array);

    [Test]
    public void Kind_ReturnsString_ForJsonString() =>
        Parse("\"Alice\"").Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_ReturnsNumber_ForJsonNumber() =>
        Parse("42").Kind.ShouldBe(DataKind.Number);

    [TestCase("true")]
    [TestCase("false")]
    public void Kind_ReturnsBoolean_ForJsonTrueOrFalse(string json) =>
        Parse(json).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_ReturnsNull_ForJsonNull() =>
        Parse("null").Kind.ShouldBe(DataKind.Null);

    [Test]
    public void TryGetProperty_ReturnsWrappedValue_WhenPropertyExists()
    {
        var source = Parse("""{"Name": "Alice"}""");

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenPropertyMissing() =>
        Parse("""{"Name": "Alice"}""").TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenNotAnObject() =>
        Parse("\"Alice\"").TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void EnumerateArray_ReturnsWrappedItems()
    {
        var items = Parse("""["a", "b"]""").EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase("true", true)]
    [TestCase("false", false)]
    public void AsBoolean_ReturnsUnderlyingValue(string json, bool expected) =>
        Parse(json).AsBoolean().ShouldBe(expected);

    [Test]
    public void AsBoolean_ReturnsFalse_ForNonBoolean() =>
        Parse("\"Alice\"").AsBoolean().ShouldBeFalse();

    [Test]
    public void AsDisplayString_ReturnsUnderlyingValueText()
    {
        Parse("\"Alice\"").AsDisplayString().ShouldBe("Alice");
        Parse("42").AsDisplayString().ShouldBe("42");
    }

    static JsonElementDataSource Parse(string json) =>
        new(JsonDocument.Parse(json).RootElement);
}
