using Humanizer;
using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal sealed class TokenNodeRenderer : INodeRenderer
{
    public string Render(INode node, JsonElement data)
    {
        var path = ((TokenNode)node).Path;
        var propertyName = path.Dehumanize();

        return data.GetProperty(propertyName).GetString() ?? string.Empty;
    }
}
