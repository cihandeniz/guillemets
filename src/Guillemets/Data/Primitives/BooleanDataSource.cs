namespace Guillemets.Data.Primitives;

internal record BooleanDataSource(bool Value)
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
        Value;

    public string? AsDisplayString() =>
        Value ? "true" : "false";
}
