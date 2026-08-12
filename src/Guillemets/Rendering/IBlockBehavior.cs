using Guillemets.Ast;

namespace Guillemets.Rendering;

internal interface IBlockBehavior
{
    string Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody);
}