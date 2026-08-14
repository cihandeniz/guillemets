using Guillemets.Rendering;

namespace Guillemets.Ast;

internal interface IRenderable
{
    bool EndsAtLineEnd => false;

    string Render(RenderContext context, Scope scope);
}