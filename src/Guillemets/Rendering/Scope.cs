using Guillemets.Data;
using Guillemets.Data.Primitives;
using Humanizer;

namespace Guillemets.Rendering;

internal record Scope(IDataSource Data,
    Scope? Parent = null,
    bool? IsFirst = null,
    bool? IsLast = null
)
{
    const string FIRST = "first";
    const string LAST = "last";

    static readonly HashSet<string> MAGIC_NAMES = [FIRST, LAST];

    public bool TryGetMagic(string property, bool negated, out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;

        var name = property.ToLowerInvariant();
        var magic = name switch
        {
            FIRST => IsFirst,
            LAST => IsLast,
            _ => null,
        };

        if (magic is not null)
        {
            value = (negated ? !magic.Value : magic.Value) ? BooleanDataSource.TRUE : BooleanDataSource.FALSE;

            return true;
        }

        return MAGIC_NAMES.Contains(name) && Parent is not null && Parent.TryGetMagic(property, negated, out value);
    }

    public Scope FindOwner(PropertyChain properties)
    {
        if (properties.Count == 0 || HasProperty(properties[0])) { return this; }

        return Parent is not null ? Parent.FindOwner(properties) : this;
    }

    bool HasProperty(string property) =>
        Data.Kind == DataKind.Object && Data.TryGetProperty(property.Dehumanize(), out _);
}