using Guillemets.Ast.Rendering;

namespace Guillemets.Ast;

internal record FilterNode(string Name, string Value)
    : INode
{
    public string Render(RenderContext context, Scope scope) =>
        throw new InvalidOperationException($"Filter '{Name}' was not consumed by its owning parser.");
}
