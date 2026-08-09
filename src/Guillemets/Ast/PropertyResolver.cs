using Humanizer;
using System.Text.Json;

namespace Guillemets.Ast;

internal class PropertyResolver
{
    public IEnumerable<JsonElement> Resolve(Scope scope, PropertyChain properties)
    {
        if (properties.Count == 1 && scope.TryGetMagic(properties[0], properties.LastSegmentNegated, out var magic))
        {
            yield return magic;
            yield break;
        }

        foreach (var result in Resolve(scope.Data, properties))
        {
            yield return result;
        }
    }

    public IReadOnlyList<JsonElement>? ResolveLoopItems(Scope scope, PropertyChain properties) =>
        ResolveItemsMatchingLastSegment(scope, properties) ?? ResolveArrayItems(scope, properties);

    IReadOnlyList<JsonElement>? ResolveItemsMatchingLastSegment(Scope scope, PropertyChain properties)
    {
        if (properties.Count <= 1) { return null; }

        var containers = Resolve(scope.Data, properties.WithoutLast()).ToList();
        if (containers.Count != 1 || containers[0].ValueKind != JsonValueKind.Array) { return null; }

        var lastSegment = new PropertyChain([properties[^1]], properties.LastSegmentNegated);

        return [.. containers[0].EnumerateArray()
            .Where(item => Resolve(item, lastSegment).SingleOrDefault().ValueKind == JsonValueKind.True)];
    }

    IReadOnlyList<JsonElement>? ResolveArrayItems(Scope scope, PropertyChain properties)
    {
        var resolved = Resolve(scope, properties).ToList();

        return resolved.Count == 1 && resolved[0].ValueKind == JsonValueKind.Array
            ? [.. resolved[0].EnumerateArray()]
            : null;
    }

    public IEnumerable<JsonElement> Resolve(JsonElement current, PropertyChain properties)
    {
        if (properties.Count == 0)
        {
            yield return current;
            yield break;
        }

        if (current.ValueKind == JsonValueKind.Null)
        {
            yield break;
        }

        if (current.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in current.EnumerateArray())
            {
                foreach (var result in Resolve(item, properties))
                {
                    yield return result;
                }
            }

            yield break;
        }

        var next = current.GetProperty(properties[0].Dehumanize());
        if (properties.Count == 1 && properties.LastSegmentNegated) { next = Negate(next); }

        foreach (var result in Resolve(next, properties.Tail()))
        {
            yield return result;
        }
    }

    static JsonElement Negate(JsonElement value) =>
        value.ValueKind == JsonValueKind.True ? JsonBooleans.FALSE : JsonBooleans.TRUE;
}