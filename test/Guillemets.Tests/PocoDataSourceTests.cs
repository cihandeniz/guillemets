using Guillemets.Data;
using Guillemets.Data.Poco;
using Shouldly;
using System.Collections.ObjectModel;

namespace Guillemets.Tests;

public class PocoDataSourceTests : DataSourceSpec
{
    static IEnumerable<object> CollectionSamples()
    {
        yield return new[] { 1, 2, 3 };
        yield return new List<int> { 1, 2, 3 };
        yield return new HashSet<int> { 1, 2, 3 };
        yield return new Collection<int> { 1, 2, 3 };
        yield return Enumerable.Range(1, 3);
    }

    protected override IDataSource CreateObjectWithFullName(string value) =>
        new PocoDataSource(new { fullName = value });

    protected override IDataSource CreateScalar(string value) =>
        new PocoDataSource(value);

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
    public void As_boolean_returns_false_for_null() =>
        new PocoDataSource(null).AsBoolean().ShouldBeFalse();

    [Test]
    public void As_display_string_returns_underlying_value_text()
    {
        new PocoDataSource("Alice").AsDisplayString().ShouldBe("Alice");
        new PocoDataSource(42).AsDisplayString().ShouldBe("42");
    }

    [Test]
    public void As_display_string_returns_null_for_null_value() =>
        new PocoDataSource(null).AsDisplayString().ShouldBeNull();

    [Test]
    public void Kind_returns_string_for_datetime_value() =>
        new PocoDataSource(DateTime.Now).Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_string_for_guid_value() =>
        new PocoDataSource(Guid.NewGuid()).Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_string_for_enum_value() =>
        new PocoDataSource(DayOfWeek.Monday).Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_string_for_date_only_value() =>
        new PocoDataSource(DateOnly.FromDateTime(DateTime.Now)).Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_returns_string_for_time_only_value() =>
        new PocoDataSource(TimeOnly.FromDateTime(DateTime.Now)).Kind.ShouldBe(DataKind.String);

    [Test]
    public void As_boolean_returns_true_for_datetime_value() =>
        new PocoDataSource(DateTime.Now).AsBoolean().ShouldBeTrue();

    [Test]
    public void Kind_returns_object_for_dictionary() =>
        new PocoDataSource(new Dictionary<string, int> { ["Age"] = 30 }).Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Try_get_property_returns_wrapped_value_for_dictionary_key()
    {
        var source = new PocoDataSource(new Dictionary<string, int> { ["Age"] = 30 });

        source.TryGetProperty("age", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("30");
    }
}