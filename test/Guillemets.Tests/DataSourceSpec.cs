using Guillemets.Data;
using Shouldly;

namespace Guillemets.Tests;

public abstract class DataSourceSpec
{
    protected abstract IDataSource CreateObjectWithFullName(string value);
    protected abstract IDataSource CreateScalar(string value);

    [Test]
    public void Try_get_property_returns_false_when_property_missing() =>
        CreateObjectWithFullName("Alice").TryGetProperty("Age", out _).ShouldBeFalse();

    [Test]
    public void Try_get_property_is_case_insensitive()
    {
        var source = CreateObjectWithFullName("Alice");

        source.TryGetProperty("FullName", out var value).ShouldBeTrue();
        value.AsDisplayString().ShouldBe("Alice");
    }

    [Test]
    public void Try_get_property_returns_false_when_not_an_object() =>
        CreateScalar("Alice").TryGetProperty("Length", out _).ShouldBeFalse();

    [Test]
    public void Enumerate_array_returns_empty_when_not_an_array() =>
        CreateScalar("Alice").EnumerateArray().ShouldBeEmpty();

    [Test]
    public void As_boolean_returns_true_for_present_non_boolean() =>
        CreateScalar("Alice").AsBoolean().ShouldBeTrue();
}