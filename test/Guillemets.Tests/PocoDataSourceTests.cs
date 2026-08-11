using Guillemets.Data;
using Guillemets.Data.Poco;
using Shouldly;
using System.Collections.ObjectModel;

namespace Guillemets.Tests;

public class PocoDataSourceTests
{
    [Test]
    public void Kind_ReturnsObject_ForPlainObject() =>
        new PocoDataSource(new { Name = "Alice" }).Kind.ShouldBe(DataKind.Object);

    [Test]
    public void Kind_ReturnsString_ForStringValue() =>
        new PocoDataSource("Alice").Kind.ShouldBe(DataKind.String);

    [Test]
    public void Kind_ReturnsNumber_ForNumericValue() =>
        new PocoDataSource(42).Kind.ShouldBe(DataKind.Number);

    [TestCase(true)]
    [TestCase(false)]
    public void Kind_ReturnsBoolean_ForBoolValue(bool value) =>
        new PocoDataSource(value).Kind.ShouldBe(DataKind.Boolean);

    [Test]
    public void Kind_ReturnsNull_ForNullValue() =>
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
    public void Kind_ReturnsArray_ForVariousCollectionTypes(object collection) =>
        new PocoDataSource(collection).Kind.ShouldBe(DataKind.Array);

    [Test]
    public void TryGetProperty_ReturnsWrappedValue_WhenPropertyExists()
    {
        var source = new PocoDataSource(new { Name = "Alice" });

        source.TryGetProperty("Name", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenPropertyMissing() =>
        new PocoDataSource(new { Name = "Alice" }).TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void TryGetProperty_ReturnsFalse_WhenNotAnObject() =>
        new PocoDataSource("Alice").TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void EnumerateArray_ReturnsWrappedItems_ForList()
    {
        var items = new PocoDataSource(new List<string> { "a", "b" }).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [Test]
    public void EnumerateArray_ReturnsWrappedItems_ForArray()
    {
        var items = new PocoDataSource(new[] { "a", "b" }).EnumerateArray().ToList();

        items.Select(item => item.AsDisplayString()).ShouldBe(["a", "b"]);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AsBoolean_ReturnsUnderlyingValue(bool value) =>
        new PocoDataSource(value).AsBoolean().ShouldBe(value);

    [Test]
    public void AsBoolean_ReturnsFalse_ForNonBoolean() =>
        new PocoDataSource("Alice").AsBoolean().ShouldBeFalse();

    [Test]
    public void AsDisplayString_ReturnsUnderlyingValueText()
    {
        new PocoDataSource("Alice").AsDisplayString().ShouldBe("Alice");
        new PocoDataSource(42).AsDisplayString().ShouldBe("42");
    }

    [Test]
    public void AsDisplayString_ReturnsNull_ForNullValue() =>
        new PocoDataSource(null).AsDisplayString().ShouldBeNull();
}