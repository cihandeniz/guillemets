using Guillemets.Ast.Rendering;

namespace Guillemets.Ast;

internal record VariableNode(PropertyChain Properties)
    : INode
{
    public string Render(RenderContext context, Scope scope) =>
        string.Join(", ", context.PropertyResolver.Resolve(scope, Properties).Select(value => value.AsDisplayString()));
}
