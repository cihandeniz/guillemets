using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record ConditionalBehavior(Scope Scope, JsonElement Value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (Value.ValueKind == JsonValueKind.True)
        {
            return context.Renderer.RenderAll(body, Scope);
        }

        return elseBody is not null ? context.Renderer.RenderAll(elseBody, Scope) : string.Empty;
    }
}