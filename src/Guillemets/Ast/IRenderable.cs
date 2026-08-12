using Guillemets.Rendering;

namespace Guillemets.Ast;

internal interface IRenderable
{
    string Render(RenderContext context, Scope scope);
}