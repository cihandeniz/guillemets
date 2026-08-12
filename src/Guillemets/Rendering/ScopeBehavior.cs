using Guillemets.Ast;
using Guillemets.Data;

namespace Guillemets.Rendering;

internal class ScopeBehavior(Scope _scope, IDataSource _value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody) =>
        context.Renderer.Render(body, new(_value, Parent: _scope));
}