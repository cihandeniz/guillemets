using Guillemets.Data;
using Guillemets.Data.Poco;
using Shouldly;
using System.Collections.ObjectModel;

namespace Guillemets.Tests;

public class PocoDataSourceTests
{
    [Test]
    public void Kind_returns_object_for_plain_object() =>
        new PocoDataSource(new { Name = "Alice" }).Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_returns_string_for_string_value() =>
        new PocoDataSource("Alice").Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_number_for_numeric_value() =>
        new PocoDataSource(42).Kind.ShouldBe(DataKind.Number);

    [TestCase(true)]
    [TestCase(false)]
    public void Kind_returns_boolean_for_bool_value(bool value) =>
        new PocoDataSource(value).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_returns_null_for_null_value() =>
        new PocoDataSource(null).Kind.ShouldBe(DataKind.Null);

    static IEnumerable<object> CollectionSamples()
    {
        yield return new[] { 1, 2, 3 };
        yield return new List<int> { 1, 2, 3 };
        yield return new HashSet<int> { 1, 2, 3 };
        yield return new Collection<int> { 1, 2, 3 };
        yield return Enumerable.Range(1, 3);
    }

    [TestCaseSource(nameof(CollectionSamples))]
    public void Kind_returns_array_for_various_collection_types(object collection) =>
        new PocoDataSource(collection).Kind.ShouldBe(DataKind.Array);

    [Test]
    public void Try_get_property_returns_wrapped_value_when_property_exists()
    {
        var source = new PocoDataSource(new { Name = "Alice" });

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void Try_get_property_returns_false_when_property_missing() =>
        new PocoDataSource(new { Name = "Alice" }).TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void Try_get_property_returns_false_when_not_an_object() =>
        new PocoDataSource("Alice").TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void Enumerate_array_returns_wrapped_items_for_list()
    {
        var items = new PocoDataSource(new List<string> { "a", "b" }).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [Test]
    public void Enumerate_array_returns_wrapped_items_for_array()
    {
        var items = new PocoDataSource(new[] { "a", "b" }).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void As_boolean_returns_underlying_value(bool value) =>
        new PocoDataSource(value).AsBoolean().ShouldBe(value);

    [Test]
    public void As_boolean_returns_false_for_non_boolean() =>
        new PocoDataSource("Alice").AsBoolean().ShouldBeFalse();

    [Test]
    public void As_display_string_returns_underlying_value_text()
    {
        new PocoDataSource("Alice").AsDisplayString().ShouldBe("Alice");
        new PocoDataSource(42).AsDisplayString().ShouldBe("42");
    }

    [Test]
    public void As_display_string_returns_null_for_null_value() =>
        new PocoDataSource(null).AsDisplayString().ShouldBeNull();
}