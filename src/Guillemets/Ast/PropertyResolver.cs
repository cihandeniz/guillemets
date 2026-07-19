using Humanizer;
using System.Text.Json;

namespace Guillemets.Ast;

internal class PropertyResolver
{
    public IEnumerable<JsonElement> Resolve(JsonElement current, PropertyChain properties)
    {
        if (properties.Count == 0)
        {
            yield return current;
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
        foreach (var result in Resolve(next, new PropertyChain([.. properties.Skip(1)])))
        {
            yield return result;
        }
    }
}