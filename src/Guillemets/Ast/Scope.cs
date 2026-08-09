using System.Text.Json;

namespace Guillemets.Ast;

internal record Scope(JsonElement Data,
    Scope? Parent = null,
    bool? IsFirst = null,
    bool? IsLast = null
)
{
    const string FIRST = "first";
    const string LAST = "last";

    static readonly HashSet<string> MAGIC_NAMES = [FIRST, LAST];

    public bool TryGetMagic(string property, bool negated, out JsonElement value)
    {
        value = default;

        var name = property.ToLowerInvariant();
        var magic = name switch
        {
            FIRST => IsFirst,
            LAST => IsLast,
            _ => null,
        };

        if (magic is not null)
        {
            value = (negated ? !magic.Value : magic.Value) ? JsonBooleans.TRUE : JsonBooleans.FALSE;

            return true;
        }

        return MAGIC_NAMES.Contains(name) && Parent is not null && Parent.TryGetMagic(property, negated, out value);
    }
}