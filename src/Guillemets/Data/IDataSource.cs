using Guillemets.Data.Primitives;

namespace Guillemets.Data;

public interface IDataSource
{
    DataKind Kind { get; }
    bool TryGetProperty(string name, out IDataSource value);
    IEnumerable<IDataSource> EnumerateArray();
    bool AsBoolean();
    string? AsDisplayString();

    IDataSource Negate() =>
        AsBoolean() ? BooleanDataSource.FALSE : BooleanDataSource.TRUE;
}