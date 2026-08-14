using Guillemets.Data.Primitives;
using System.Text.Json;

namespace Guillemets.Data.Json;

/// <summary>
/// Adapts <see cref="JsonElement"/> — case-insensitive
/// property lookup. Every member is <see langword="virtual"/> — subclass
/// to override just one piece of behavior instead of reimplementing
/// <see cref="IDataSource"/> from scratch.
/// </summary>
public class JsonElementDataSource(JsonElement _element)
    : IDataSource
{
    /// <inheritdoc/>
    public virtual DataKind Kind => _element.ValueKind switch
    {
        JsonValueKind.Object => DataKind.Object,
        JsonValueKind.Array => DataKind.Array,
        JsonValueKind.String => DataKind.String,
        JsonValueKind.Number => DataKind.Number,
        JsonValueKind.True or JsonValueKind.False => DataKind.Boolean,
        JsonValueKind.Null => DataKind.Null,
        JsonValueKind.Undefined => DataKind.Undefined,
        _ => throw new ArgumentOutOfRangeException(nameof(_element), _element.ValueKind, "Unrecognized JSON value kind."),
    };

    /// <inheritdoc/>
    public virtual bool TryGetProperty(string name, out IDataSource value)
    {
        if (Kind == DataKind.Object)
        {
            foreach (var property in _element.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) { continue; }

                value = new JsonElementDataSource(property.Value);

                return true;
            }
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    /// <inheritdoc/>
    public virtual IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? _element.EnumerateArray().Select(item => (IDataSource)new JsonElementDataSource(item))
            : [];

    /// <inheritdoc/>
    public virtual bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => _element.GetBoolean(),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(_element), Kind, "Unrecognized data kind."),
    };

    /// <inheritdoc/>
    public virtual string? AsDisplayString() =>
        _element.ToString();
}