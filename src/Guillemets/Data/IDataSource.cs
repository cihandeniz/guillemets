namespace Guillemets.Data;

public interface IDataSource
{
    DataKind Kind { get; }
    bool TryGetProperty(string name, out IDataSource value);
    IEnumerable<IDataSource> EnumerateArray();
    bool AsBoolean();
    string? AsDisplayString();
}