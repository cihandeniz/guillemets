using Guillemets.Data;
using Guillemets.Data.Primitives;
using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChainNode Properties, IReadOnlyList<IRenderable> Body,
    IReadOnlyList<IRenderable>? ElseBody = null,
    string? VariableName = null
) : IRenderable
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