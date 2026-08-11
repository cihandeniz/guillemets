using Guillemets.Ast;
using Guillemets.Data;

namespace Guillemets.Rendering;

internal class ConditionalBehavior(Scope _scope, IDataSource _value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (_value.AsBoolean())
        {
            return context.Renderer.RenderAll(body, _scope);
        }

        return elseBody is not null ? context.Renderer.RenderAll(elseBody, _scope) : string.Empty;
    }
}