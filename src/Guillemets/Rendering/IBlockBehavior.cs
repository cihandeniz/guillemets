using Guillemets.Ast;

namespace Guillemets.Rendering;

internal interface IBlockBehavior
{
    string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody);
}