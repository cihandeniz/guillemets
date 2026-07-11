using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal abstract class NodeRendererBase<TNode> : INodeRenderer<TNode>
    where TNode : INode
{
    public abstract string Render(TNode node, JsonElement data);

    string INodeRenderer.Render(INode node, JsonElement data) =>
        Render((TNode)node, data);
}
