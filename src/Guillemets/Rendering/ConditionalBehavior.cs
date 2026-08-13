using Guillemets.Ast;
using Guillemets.Data;

namespace Guillemets.Rendering;

internal class ConditionalBehavior(Scope _scope, IDataSource _value)
    : IBlockBehavior
{
    public IEnumerable<string> Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody)
    {
        if (_value.AsBoolean())
        {
            return [context.Renderer.Render(body, _scope)];
        }

        return elseBody is not null
            ? [context.Renderer.Render(elseBody, _scope)]
            : [];
    }
}