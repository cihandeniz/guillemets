using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal sealed class TokenNodeRenderer : INodeRenderer
{
    public string Render(INode node, JsonElement data)
    {
        var path = ((TokenNode)node).Path;
        var propertyName = char.ToUpperInvariant(path[0]) + path[1..];

        return data.GetProperty(propertyName).GetString() ?? string.Empty;
    }
}
