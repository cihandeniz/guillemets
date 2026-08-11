using Guillemets.Filters;
using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record FilterNode(IFilter Filter, IReadOnlyList<string> Args)
    : INode
{
    public string Render(RenderContext context, Scope scope) =>
        throw new InvalidOperationException($"Filter '{Filter.GetType().Name}' was not consumed by its owning parser.");
}