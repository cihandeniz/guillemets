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
        if (Kind == DataKind.Object && Element.TryGetProperty(name, out var property))
        {
            value = new JsonElementDataSource(property);

            return true;
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        Element.EnumerateArray().Select(item => (IDataSource)new JsonElementDataSource(item));

    public bool AsBoolean() =>
        Kind == DataKind.Boolean && Element.GetBoolean();

    public string? AsDisplayString() =>
        Element.ToString();
}