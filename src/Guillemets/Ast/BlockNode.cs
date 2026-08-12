using Guillemets.Data;
using Guillemets.Data.Primitives;
using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChainNode Properties, IReadOnlyList<IRenderable> Body,
    IReadOnlyList<IRenderable>? ElseBody = null,
    string? VariableName = null,
    IReadOnlyList<FilterNode>? Footer = null
) : IRenderable
{
    public string Render(RenderContext context, Scope scope)
    {
        var rendered = ResolveBehavior(context, scope).Render(context, Body, ElseBody);
        if (VariableName is null) { return rendered; }

        context.Variables.Define(VariableName, rendered.TrimEnd(NEWLINE));

        return string.Empty;
    }

    // TODO all behaviors need to have filters, so block behavior better return a
    // string enumerable instead of just a string, so that block node handles the
    // rest for all
    //
    // for single item behaviors (scope and conditional) join and join last will
    // have no effect, since result is single or no item, for other filters, it
    // will apply to all items
    IBlockBehavior ResolveBehavior(RenderContext context, Scope scope)
    {
        if (context.PropertyResolver.TryResolveLoopItems(scope, Properties, out var items))
        {
            return new LoopBehavior(scope, items, Footer ?? []);
        }

        var value = context.PropertyResolver.Resolve(scope, Properties).SingleOrDefault() ?? UndefinedDataSource.INSTANCE;
        if (value.Kind == DataKind.Object)
        {
            return new ScopeBehavior(scope, value);
        }

        return new ConditionalBehavior(scope, value);
    }
}