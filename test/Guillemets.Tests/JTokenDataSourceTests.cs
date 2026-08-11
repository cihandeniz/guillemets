using Guillemets.Data;
using Guillemets.Data.Newtonsoft;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace Guillemets.Tests;

public class JTokenDataSourceTests
{
    [Test]
    public void Kind_ReturnsObject_ForJObject() =>
        Wrap(new JObject()).Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_ReturnsArray_ForJArray() =>
        Wrap(new JArray()).Kind.ShouldBe(DataKind.Array);

    [Test]
    public void Kind_ReturnsString_ForJValueString() =>
        Wrap(new JValue("Alice")).Kind.ShouldBe(DataKind.String);

    [TestCase(42)]
    [TestCase(4.2)]
    public void Kind_ReturnsNumber_ForJValueNumber(double value) =>
        Wrap(new JValue(value)).Kind.ShouldBe(DataKind.Number);

    [TestCase(true)]
    [TestCase(false)]
    public void Kind_ReturnsBoolean_ForJValueBoolean(bool value) =>
        Wrap(new JValue(value)).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_ReturnsNull_ForJValueNull() =>
        Wrap(JValue.CreateNull()).Kind.ShouldBe(DataKind.Null);

    [Test]
    public void TryGetProperty_ReturnsWrappedValue_WhenPropertyExists()
    {
        var source = Wrap(new JObject { ["Name"] = "Alice" });

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenPropertyMissing() =>
        Wrap(new JObject { ["Name"] = "Alice" }).TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenNotAnObject() =>
        Wrap(new JValue("Alice")).TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void EnumerateArray_ReturnsWrappedItems()
    {
        var items = Wrap(new JArray("a", "b")).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AsBoolean_ReturnsUnderlyingValue(bool value) =>
        Wrap(new JValue(value)).AsBoolean().ShouldBe(value);

    [Test]
    public void AsBoolean_ReturnsFalse_ForNonBoolean() =>
        Wrap(new JValue("Alice")).AsBoolean().ShouldBeFalse();

    [Test]
    public void AsDisplayString_ReturnsUnderlyingValueText()
    {
        Wrap(new JValue("Alice")).AsDisplayString().ShouldBe("Alice");
        Wrap(new JValue(42)).AsDisplayString().ShouldBe("42");
    }

    static JTokenDataSource Wrap(JToken token) =>
        new(token);
}