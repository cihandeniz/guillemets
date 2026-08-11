using Guillemets.Data.Primitives;
using System.Collections;

namespace Guillemets.Data.Poco;

public record PocoDataSource(object? Value)
    : IDataSource
{
    public DataKind Kind => Value switch
    {
        null => DataKind.Null,
        bool => DataKind.Boolean,
        string => DataKind.String,
        sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
            => DataKind.Number,
        IEnumerable => DataKind.Array,
        _ => DataKind.Object,
    };

    public bool TryGetProperty(string name, out IDataSource value)
    {
        var property = Kind == DataKind.Object ? Value?.GetType().GetProperty(name) : null;
        if (property is null)
        {
            value = UndefinedDataSource.INSTANCE;

            return false;
        }

        value = new PocoDataSource(property.GetValue(Value));

        return true;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? ((IEnumerable)(Value ?? throw new InvalidOperationException("Array value was unexpectedly null.")))
                .Cast<object?>().Select(item => (IDataSource)new PocoDataSource(item))
            : [];

    public bool AsBoolean() =>
        Kind == DataKind.Boolean && (bool)(Value ?? throw new InvalidOperationException("Boolean value was unexpectedly null."));

    public string? AsDisplayString() =>
        Value?.ToString();
}