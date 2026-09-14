using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record PipeNode(string Text)
    : IRenderable
{
    public string Render(RenderContext context, Scope scope) =>
        Text;
}