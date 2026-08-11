namespace Guillemets.Data.Primitives;

internal record StringDataSource(string Value)
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
        false;

    public string? AsDisplayString() =>
        Value;
}