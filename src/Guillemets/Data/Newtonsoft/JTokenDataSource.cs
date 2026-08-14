using Guillemets.Data.Primitives;
using Newtonsoft.Json.Linq;

namespace Guillemets.Data.Newtonsoft;

/// <summary>
/// Adapts Newtonsoft's <see cref="JToken"/> — case-insensitive property
/// lookup. Every member is <see langword="virtual"/> — subclass to
/// override just one piece of behavior instead of reimplementing
/// <see cref="IDataSource"/> from scratch.
/// </summary>
public class JTokenDataSource(JToken _element)
    : IDataSource
{
    /// <inheritdoc/>
    public virtual DataKind Kind => _element.Type switch
    {
        JTokenType.Object => DataKind.Object,
        JTokenType.Array => DataKind.Array,
        JTokenType.String => DataKind.String,
        JTokenType.Integer => DataKind.Number,
        JTokenType.Float => DataKind.Number,
        JTokenType.Boolean => DataKind.Boolean,
        JTokenType.Null => DataKind.Null,
        JTokenType.Date => DataKind.String,
        JTokenType.Raw => DataKind.String,
        JTokenType.Bytes => DataKind.String,
        JTokenType.Guid => DataKind.String,
        JTokenType.Uri => DataKind.String,
        JTokenType.TimeSpan => DataKind.String,
        JTokenType.Undefined => DataKind.Undefined,
        JTokenType.None => DataKind.Undefined,
        JTokenType.Constructor => DataKind.Undefined,
        JTokenType.Property => DataKind.Undefined,
        JTokenType.Comment => DataKind.Undefined,
        _ => throw new ArgumentOutOfRangeException(nameof(_element), _element.Type, "Unrecognized JToken type."),
    };

    /// <inheritdoc/>
    public virtual bool TryGetProperty(string name, out IDataSource value)
    {
        if (Kind == DataKind.Object && _element is JObject obj && obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var property))
        {
            value = new JTokenDataSource(property);

            return true;
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    /// <inheritdoc/>
    public virtual IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? _element.Children().Select(item => (IDataSource)new JTokenDataSource(item))
            : [];

    /// <inheritdoc/>
    public virtual bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => _element.Value<bool>(),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(_element), Kind, "Unrecognized data kind."),
    };

    /// <inheritdoc/>
    public virtual string? AsDisplayString() =>
        _element.ToString();
}