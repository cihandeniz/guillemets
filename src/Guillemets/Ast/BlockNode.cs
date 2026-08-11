using Guillemets.Ast.Rendering;
using Guillemets.Data;
using Guillemets.Data.Primitives;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChain Properties, IReadOnlyList<INode> Body,
    IReadOnlyList<INode>? ElseBody = null,
    string? VariableName = null,
    string? Separator = null
) : INode
{
    public string Render(RenderContext context, Scope scope)
    {
        var rendered = ResolveBehavior(context, scope).Render(context, Body, ElseBody);
        if (VariableName is null) { return rendered; }

        context.Variables.Define(VariableName, rendered.TrimEnd(NEWLINE));

        return string.Empty;
    }

    IBlockBehavior ResolveBehavior(RenderContext context, Scope scope)
    {
        var items = context.PropertyResolver.ResolveLoopItems(scope, Properties);
        if (items is not null)
        {
            return new LoopBehavior(scope, items, Separator);
        }

        var value = context.PropertyResolver.Resolve(scope, Properties).SingleOrDefault() ?? UndefinedDataSource.INSTANCE;
        if (value.Kind == DataKind.Object)
        {
            return new ScopeBehavior(scope, value);
        }

        return new ConditionalBehavior(scope, value);
    }
}