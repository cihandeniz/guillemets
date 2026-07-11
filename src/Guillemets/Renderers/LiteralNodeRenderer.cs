using Guillemets.Ast;
using System.Text.Json;

namespace Guillemets.Renderers;

internal sealed class LiteralNodeRenderer : NodeRendererBase<LiteralNode>
{
    public override string Render(LiteralNode node, JsonElement data) =>
        node.Text;
}
