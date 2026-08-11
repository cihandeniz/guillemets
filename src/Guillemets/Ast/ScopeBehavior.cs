using Guillemets.Ast.Rendering;
using Guillemets.Data;

namespace Guillemets.Ast;

internal record ScopeBehavior(Scope Scope, IDataSource Value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody) =>
        context.Renderer.RenderAll(body, new Scope(Value, Parent: Scope));
}
