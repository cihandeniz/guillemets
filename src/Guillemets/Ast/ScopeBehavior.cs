using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record ScopeBehavior(Scope Scope, JsonElement Value)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody) =>
        context.Renderer.RenderAll(body, new Scope(Value, Parent: Scope));
}