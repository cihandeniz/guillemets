namespace Guillemets.Data.Primitives;

internal class StringDataSource(string _value)
    : IDataSource
{
    public DataKind Kind => DataKind.String;

    public bool TryGetProperty(string name, out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        [];

    public bool AsBoolean() =>
        true;

    public string? AsDisplayString() =>
        _value;
}