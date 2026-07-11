using Guillemets.Ast;
using Humanizer;
using System.Text.Json;

namespace Guillemets.Renderers;

internal sealed class TokenNodeRenderer : NodeRendererBase<TokenNode>
{
    public override string Render(TokenNode node, JsonElement data)
    {
        var current = data;
        foreach (var segment in node.Segments)
        {
            current = current.GetProperty(segment.Dehumanize());
        }

        return current.GetString() ?? string.Empty;
    }
}
