using Guillemets.Data;
using Guillemets.Data.Primitives;
using Guillemets.Filters;
using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChainNode Properties, IReadOnlyList<IRenderable> Body,
    IReadOnlyList<IRenderable>? ElseBody = null,
    string? VariableName = null,
    IReadOnlyList<FilterNode>? Footer = null
) : IRenderable
{
    public bool EndsAtLineEnd =>
        true;

    public string Render(RenderContext context, Scope scope)
    {
        var items = ResolveBehavior(context, scope).Render(context, Body, ElseBody);
        var rendered = string.Concat(ApplyFooter(items));
        if (VariableName is null) { return rendered; }

        scope.DefineVariable(VariableName, rendered.TrimEnd(NEWLINE));

        return string.Empty;
    }

    IEnumerable<string> ApplyFooter(IEnumerable<string> items)
    {
        if (Footer is not { Count: > 0 }) { return items; }

        var values = items.Select(item => item.TrimEnd(NEWLINE));
        foreach (var filter in Footer)
        {
            values = filter.Apply(values, FilterContext.Footer);
        }

        return values.Select(value => value + NEWLINE);
    }

    IBlockBehavior ResolveBehavior(RenderContext context, Scope scope)
    {
        if (context.PropertyResolver.TryResolveLoopItems(scope, Properties, out var items))
        {
            return new LoopBehavior(scope, items);
        }

        var value = context.PropertyResolver.Resolve(scope, Properties).SingleOrDefault() ?? UndefinedDataSource.INSTANCE;
        if (value.Kind == DataKind.Object)
        {
            return new ScopeBehavior(scope, value);
        }

        return new ConditionalBehavior(scope, value);
    }
}