using Guillemets.Data.Primitives;

namespace Guillemets.Data;

/// <summary>
/// Adapts one external data format to the engine — object/array/scalar
/// shape, property lookup, boolean coercion, display string. The built-in
/// adapters are <c>PocoDataSource</c>, <c>JsonElementDataSource</c>, and
/// <c>JTokenDataSource</c>; implement this interface to add another.
/// </summary>
public interface IDataSource
{
    /// <summary>
    /// This value's shape, driving block-behavior inference and truthiness.
    /// </summary>
    DataKind Kind { get; }

    /// <summary>
    /// Looks up a property by name, matched case-insensitively regardless
    /// of the underlying model's own naming convention (see specs.md's
    /// Variables section).
    /// </summary>
    /// <param name="name">The property name to look up.</param>
    /// <param name="value">
    /// The property's value if found; an "undefined" sentinel otherwise.
    /// </param>
    /// <returns><see langword="true"/> if the property was found.</returns>
    bool TryGetProperty(string name, out IDataSource value);

    /// <summary>
    /// Enumerates this value's items when <see cref="Kind"/> is
    /// <see cref="DataKind.Array"/>; empty otherwise.
    /// </summary>
    IEnumerable<IDataSource> EnumerateArray();

    /// <summary>
    /// This value's truthiness (see specs.md's Blocks section for the
    /// exact rules per <see cref="DataKind"/>).
    /// </summary>
    bool AsBoolean();

    /// <summary>
    /// This value's rendered text, or <see langword="null"/> for a value
    /// with no display form (e.g. an object).
    /// </summary>
    string? AsDisplayString();

    /// <summary>
    /// Boolean-negates this value — used by the <c>!</c> negation prefix.
    /// </summary>
    IDataSource Negate() =>
        AsBoolean() ? BooleanDataSource.FALSE : BooleanDataSource.TRUE;
}