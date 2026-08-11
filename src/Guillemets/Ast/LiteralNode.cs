using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record LiteralNode(string Text)
    : INode
{
    public string Render(RenderContext context, Scope scope) =>
        Text;
}