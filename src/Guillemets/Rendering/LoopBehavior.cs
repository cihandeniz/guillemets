using Guillemets.Ast;
using Guillemets.Data;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal record LoopBehavior(Scope Scope, IReadOnlyList<IDataSource> Items,
    string? Separator = null
) : IBlockBehavior
{
    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (!Items.Any())
        {
            return elseBody is not null
                ? context.Renderer.RenderAll(elseBody, Scope)
                : string.Empty;
        }

        var renders = new List<string>(Items.Count);
        for (var i = 0; i < Items.Count; i++)
        {
            var itemScope = new Scope(Items[i],
                Parent: Scope,
                IsFirst: i == 0,
                IsLast: i == Items.Count - 1
            );
            renders.Add(context.Renderer.RenderAll(body, itemScope));
        }

        return Separator is null
            ? string.Concat(renders)
            : string.Join(Separator, renders.Select(render => render.TrimEnd(NEWLINE)));
    }
}