using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChain Properties, IReadOnlyList<INode> Body,
    IReadOnlyList<INode>? ElseBody = null
) : INode
{
    public string Render(RenderContext context, Scope scope) =>
        ResolveBehavior(context, scope).Render(context, Body, ElseBody);

    IBlockBehavior ResolveBehavior(RenderContext context, Scope scope)
    {
        var items = context.PropertyResolver.ResolveLoopItems(scope, Properties);
        if (items is not null)
        {
            return new LoopBehavior(scope, items);
        }

        var value = context.PropertyResolver.Resolve(scope, Properties).SingleOrDefault();
        if (value.ValueKind == JsonValueKind.Object)
        {
            return new ScopeBehavior(scope, value);
        }

        return new ConditionalBehavior(scope, value);
    }
}