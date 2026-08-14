using Guillemets.Data;
using Guillemets.Data.Primitives;

namespace Guillemets.Rendering;

internal class VariableStore
{
    readonly Dictionary<string, IDataSource> _values = [];

    public void Define(string name, string value) =>
        _values[name.Dehumanize()] = new StringDataSource(value);

    public bool TryResolve(string name, out IDataSource value)
    {
        if (_values.TryGetValue(name.Dehumanize(), out var found))
        {
            value = found;

            return true;
        }

        value = UndefinedDataSource.INSTANCE;

        return false;
    }
}