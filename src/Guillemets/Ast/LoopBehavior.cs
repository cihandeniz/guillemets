using Guillemets.Ast.Rendering;
using System.Text;
using System.Text.Json;

namespace Guillemets.Ast;

internal record LoopBehavior(Scope Scope, IReadOnlyList<JsonElement> Items)
    : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (!Items.Any())
        {
            return elseBody is not null
                ? context.Renderer.RenderAll(elseBody, Scope)
                : string.Empty;
        }

        var result = new StringBuilder();
        for (var i = 0; i < Items.Count; i++)
        {
            var itemScope =
                new Scope(Items[i],
                    Parent: Scope,
                    IsFirst: i == 0,
                    IsLast: i == Items.Count - 1
                );
            result.Append(context.Renderer.RenderAll(body, itemScope));
        }

        return result.ToString();
    }
}