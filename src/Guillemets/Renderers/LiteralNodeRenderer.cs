using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal sealed class LiteralNodeRenderer : INodeRenderer
{
    public string Render(INode node, JsonElement data) =>
        ((LiteralNode)node).Text;
}
