using Guillemets.Ast;

namespace Guillemets.Rendering;

internal interface IBlockBehavior
{
    IEnumerable<string> Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody);
}