using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal interface INodeRenderer
{
    string Render(INode node, JsonElement data);
}

internal interface INodeRenderer<TNode> : INodeRenderer
    where TNode : INode
{
    string Render(TNode node, JsonElement data);
}
