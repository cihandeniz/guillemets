using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record LiteralNode(string Text)
    : IRenderable
{
    public string Render(RenderContext context, Scope scope) =>
        Text;
}