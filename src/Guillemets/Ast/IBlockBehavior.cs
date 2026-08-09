using Guillemets.Ast.Rendering;

namespace Guillemets.Ast;

internal interface IBlockBehavior
{
    string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody);
}