using Guillemets.Data;
using Guillemets.Data.Newtonsoft;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace Guillemets.Tests;

public class JTokenDataSourceTests
{
    [Test]
    public void Kind_returns_object_for_j_object() =>
        Wrap(new JObject()).Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_returns_array_for_j_array() =>
        Wrap(new JArray()).Kind.ShouldBe(DataKind.Array);

    [Test]
    public void Kind_returns_string_for_j_value_string() =>
        Wrap(new JValue("Alice")).Kind.ShouldBe(DataKind.String);

    [TestCase(42)]
    [TestCase(4.2)]
    public void Kind_returns_number_for_j_value_number(double value) =>
        Wrap(new JValue(value)).Kind.ShouldBe(DataKind.Number);

    [TestCase(true)]
    [TestCase(false)]
    public void Kind_returns_boolean_for_j_value_boolean(bool value) =>
        Wrap(new JValue(value)).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_returns_null_for_j_value_null() =>
        Wrap(JValue.CreateNull()).Kind.ShouldBe(DataKind.Null);

    [Test]
    public void Try_get_property_returns_wrapped_value_when_property_exists()
    {
        var source = Wrap(new JObject { ["Name"] = "Alice" });

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void Try_get_property_returns_false_when_property_missing() =>
        Wrap(new JObject { ["Name"] = "Alice" }).TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void Try_get_property_returns_false_when_not_an_object() =>
        Wrap(new JValue("Alice")).TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void Enumerate_array_returns_wrapped_items()
    {
        var items = Wrap(new JArray("a", "b")).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void As_boolean_returns_underlying_value(bool value) =>
        Wrap(new JValue(value)).AsBoolean().ShouldBe(value);

    [Test]
    public void As_boolean_returns_true_for_present_non_boolean() =>
        Wrap(new JValue("Alice")).AsBoolean().ShouldBeTrue();

    [Test]
    public void As_boolean_returns_false_for_null() =>
        Wrap(JValue.CreateNull()).AsBoolean().ShouldBeFalse();

    [Test]
    public void As_display_string_returns_underlying_value_text()
    {
        Wrap(new JValue("Alice")).AsDisplayString().ShouldBe("Alice");
        Wrap(new JValue(42)).AsDisplayString().ShouldBe("42");
    }

    static JTokenDataSource Wrap(JToken token) =>
        new(token);
}