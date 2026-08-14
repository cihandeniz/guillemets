using Guillemets.Data.Primitives;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Guillemets.Data.Poco;

public record PocoDataSource(object? Value)
    : IDataSource
{
    public DataKind Kind => Value switch
    {
        null => DataKind.Null,
        bool => DataKind.Boolean,
        string or
        DateTime or
        Guid or
        Enum => DataKind.String,
        sbyte or
        byte or
        short or
        ushort or
        int or
        uint or
        long or
        ulong or
        float or
        double or
        decimal => DataKind.Number,
        IDictionary => DataKind.Object,
        IEnumerable => DataKind.Array,
        _ => DataKind.Object,
    };

    public bool TryGetProperty(string name, out IDataSource value)
    {
        if (Value is IDictionary dictionary)
        {
            return TryGetDictionaryEntry(dictionary, name, out value);
        }

        var property = Kind == DataKind.Object
            ? Value?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            : null;
        if (property is null)
        {
            value = UndefinedDataSource.INSTANCE;

            return false;
        }

        value = new PocoDataSource(property.GetValue(Value));

        return true;
    }

    static bool TryGetDictionaryEntry(IDictionary dictionary, string name, out IDataSource value)
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string key || !string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) { continue; }

            value = new PocoDataSource(entry.Value);

            return true;
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? ((IEnumerable)(Value ?? throw new InvalidOperationException("Array value was unexpectedly null.")))
                .Cast<object?>().Select(item => (IDataSource)new PocoDataSource(item))
            : [];

    public bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => (bool)(Value ?? throw new InvalidOperationException("Boolean value was unexpectedly null.")),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(Value), Kind, "Unrecognized data kind."),
    };

    public string? AsDisplayString() =>
        Value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : Value?.ToString();
}