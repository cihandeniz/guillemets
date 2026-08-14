using Guillemets.Data.Primitives;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Guillemets.Data.Poco;

public class PocoDataSource(object? _value)
    : IDataSource
{
    static bool IsParsable(object value) =>
        value.GetType().GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IParsable<>));

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

    public virtual DataKind Kind => _value switch
    {
        null => DataKind.Null,
        bool => DataKind.Boolean,
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
        _ when IsParsable(_value) => DataKind.String,
        IDictionary => DataKind.Object,
        IEnumerable => DataKind.Array,
        _ => DataKind.Object,
    };

    public virtual bool TryGetProperty(string name, out IDataSource value)
    {
        if (_value is IDictionary dictionary)
        {
            return TryGetDictionaryEntry(dictionary, name, out value);
        }

        var property = Kind == DataKind.Object
            ? _value?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            : null;
        if (property is null)
        {
            value = UndefinedDataSource.INSTANCE;

            return false;
        }

        value = new PocoDataSource(property.GetValue(_value));

        return true;
    }

    public virtual IEnumerable<IDataSource> EnumerateArray() =>
        Kind == DataKind.Array
            ? ((IEnumerable)(_value ?? throw new InvalidOperationException("Array value was unexpectedly null.")))
                .Cast<object?>().Select(item => (IDataSource)new PocoDataSource(item))
            : [];

    public virtual bool AsBoolean() => Kind switch
    {
        DataKind.Boolean => (bool)(_value ?? throw new InvalidOperationException("Boolean value was unexpectedly null.")),
        DataKind.String or DataKind.Number => true,
        DataKind.Object or DataKind.Array or DataKind.Null or DataKind.Undefined => false,
        _ => throw new ArgumentOutOfRangeException(nameof(_value), Kind, "Unrecognized data kind."),
    };

    public virtual string? AsDisplayString() =>
        _value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : _value?.ToString();
}