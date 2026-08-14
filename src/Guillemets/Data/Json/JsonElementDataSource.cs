using Guillemets.Data.Primitives;
using System.Text.Json;

namespace Guillemets.Data.Json;

public record JsonElementDataSource(JsonElement Element)
    : IDataSource
{
    public DataKind Kind => Element.ValueKind switch
    {
        JsonValueKind.Object => DataKind.Object,
        JsonValueKind.Array => DataKind.Array,
        JsonValueKind.String => DataKind.String,
        JsonValueKind.Number => DataKind.Number,
        JsonValueKind.True or JsonValueKind.False => DataKind.Boolean,
        JsonValueKind.Null => DataKind.Null,
        JsonValueKind.Undefined => DataKind.Undefined,
        _ => throw new ArgumentOutOfRangeException(nameof(Element), Element.ValueKind, "Unrecognized JSON value kind."),
    };

    public bool TryGetProperty(string name, out IDataSource value)
    {
        if (Kind == DataKind.Object)
        {
            foreach (var property in Element.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) { continue; }

                value = new JsonElementDataSource(property.Value);

                return true;
            }
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? Element.EnumerateArray().Select(item => (IDataSource)new JsonElementDataSource(item))
            : [];

    public bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => Element.GetBoolean(),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(Element), Kind, "Unrecognized data kind."),
    };

    public string? AsDisplayString() =>
        Element.ToString();
}