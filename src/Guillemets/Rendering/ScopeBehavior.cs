using Guillemets.Ast;
using Guillemets.Data;

namespace Guillemets.Rendering;

internal class ScopeBehavior(Scope _scope, IDataSource _value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody) =>
        context.Renderer.RenderAll(body, new Scope(_value, Parent: _scope));
}