using System.Text.Json;

namespace Guillemets.Ast;

internal record Scope(JsonElement Data,
    bool? IsFirst = null,
    bool? IsLast = null
)
{
    public bool TryGetMagic(string property, bool negated, out JsonElement value)
    {
        value = default;

        var magic = property.ToLowerInvariant() switch
        {
            "first" => IsFirst,
            "last" => IsLast,
            _ => null,
        };

        if (magic is null) { return false; }

        value = (negated ? !magic.Value : magic.Value) ? JsonBooleans.TRUE : JsonBooleans.FALSE;

        return true;
    }
}