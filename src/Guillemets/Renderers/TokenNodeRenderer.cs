using Guillemets.Ast;
using Humanizer;
using System.Text.Json;

namespace Guillemets.Renderers;

internal sealed class TokenNodeRenderer : NodeRendererBase<TokenNode>
{
    public override string Render(TokenNode node, JsonElement data) =>
        string.Join(", ", Resolve(data, node.Segments).Select(value => value.ToString()));

    static IEnumerable<JsonElement> Resolve(JsonElement current, IReadOnlyList<string> segments)
    {
        if (segments.Count == 0)
        {
            yield return current;
            yield break;
        }

        if (current.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in current.EnumerateArray())
            {
                foreach (var result in Resolve(item, segments))
                {
                    yield return result;
                }
            }

            yield break;
        }

        var next = current.GetProperty(segments[0].Dehumanize());
        foreach (var result in Resolve(next, segments.Skip(1).ToList()))
        {
            yield return result;
        }
    }
}
