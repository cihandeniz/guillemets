using Guillemets.Data.Primitives;
using Newtonsoft.Json.Linq;

namespace Guillemets.Data.Newtonsoft;

public record JTokenDataSource(JToken Element)
    : IDataSource
{
    public DataKind Kind => Element.Type switch
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
        _ => throw new ArgumentOutOfRangeException(nameof(Element), Element.Type, "Unrecognized JToken type."),
    };

    public bool TryGetProperty(string name, out IDataSource value)
    {
        if (Kind == DataKind.Object && Element is JObject obj && obj.TryGetValue(name, out var property))
        {
            value = new JTokenDataSource(property);

            return true;
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? Element.Children().Select(item => (IDataSource)new JTokenDataSource(item))
            : [];

    public bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => Element.Value<bool>(),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(Element), Kind, "Unrecognized data kind."),
    };

    public string? AsDisplayString() =>
        Element.ToString();
}