using Guillemets.Ast;

namespace Guillemets.Rendering;

internal interface IBlockBehavior
{
    // TODO this will change to render multi string to allow filtering outside of block behavior
    string Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody);
}