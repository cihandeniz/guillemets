using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record TableCell(PipeNode Open, IReadOnlyList<IRenderable> Content)
    : IRenderable
{
    public string Render(RenderContext context, Scope scope) =>
        Open.Render(context, scope) + context.Renderer.Render(Content, scope, inTableCell: true);
}