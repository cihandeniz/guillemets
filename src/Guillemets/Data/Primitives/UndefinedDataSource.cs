namespace Guillemets.Data.Primitives;

internal class UndefinedDataSource : IDataSource
{
    public static readonly IDataSource INSTANCE = new UndefinedDataSource();

    public DataKind Kind => DataKind.Undefined;

    public bool TryGetProperty(string name, out IDataSource value)
    {
        value = INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        [];

    public bool AsBoolean() =>
        false;

    public string? AsDisplayString() =>
        null;
}