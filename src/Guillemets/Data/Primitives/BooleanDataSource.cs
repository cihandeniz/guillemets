namespace Guillemets.Data.Primitives;

internal class BooleanDataSource(bool _value)
    : IDataSource
{
    public static readonly IDataSource TRUE = new BooleanDataSource(true);
    public static readonly IDataSource FALSE = new BooleanDataSource(false);

    public DataKind Kind => DataKind.Boolean;

    public bool TryGetProperty(string name, out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;

        return false;
    }

    public IEnumerable<IDataSource> EnumerateArray() =>
        [];

    public bool AsBoolean() =>
        _value;

    public string? AsDisplayString() =>
        _value ? "true" : "false";
}