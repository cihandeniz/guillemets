using Guillemets.Ast.Rendering;
using Guillemets.Data;

namespace Guillemets.Ast;

internal record ConditionalBehavior(Scope Scope, IDataSource Value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (Value.AsBoolean())
        {
            return context.Renderer.RenderAll(body, Scope);
        }

        return elseBody is not null ? context.Renderer.RenderAll(elseBody, Scope) : string.Empty;
    }
}
