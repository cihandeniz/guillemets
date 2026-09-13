using Guillemets.Data;
using Guillemets.Data.Primitives;
using Guillemets.Filters;
using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChainNode Properties, IReadOnlyList<IRenderable> Body,
    IReadOnlyList<IRenderable>? ElseBody = null,
    string? VariableName = null,
    IReadOnlyList<FilterNode>? Footer = null,
    bool SwallowedBlankLineAfterClose = false
) : IRenderable
{
    static readonly string BLANK_LINE = new(NEWLINE, 2);
    static readonly string BLANK_LINE_SEPARATOR = NEWLINE.ToString();

    static string WithoutBodyPadding(string item) =>
        item.EndsWith(NEWLINE) ? item[..^1] : item;

    static bool SpansParagraphs(string body) =>
        body.Contains(BLANK_LINE, StringComparison.Ordinal);

    readonly TableBody? _table = TableBody.From(Body);

    public string Render(RenderContext context, Scope scope)
    {
        var items = ResolveBehavior(context, scope).Render(context, Body, ElseBody);
        var rendered = JoinItems(items);
        if (VariableName is null) { return RestoreBlankLineAfterClose(rendered); }

        scope.DefineVariable(VariableName, rendered.TrimEnd(NEWLINE));

        return string.Empty;
    }

    string RestoreBlankLineAfterClose(string rendered) =>
        SwallowedBlankLineAfterClose && rendered.Length > 0 ? rendered + NEWLINE : rendered;

    string JoinItems(IEnumerable<string> items)
    {
        if (Footer is { Count: > 0 }) { return string.Concat(ApplyFooter(items)); }

        var bodies = items.Select(WithoutBodyPadding).ToList();

        return string.Join(bodies.Exists(SpansParagraphs) ? BLANK_LINE_SEPARATOR : string.Empty, bodies);
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
        if (context.PropertyResolver.TryResolveLoopItems(scope, Properties, out var items, out var resolved))
        {
            return new LoopBehavior(scope, items, _table);
        }

        var value = resolved.SingleOrDefault() ?? UndefinedDataSource.INSTANCE;
        if (value.Kind == DataKind.Object)
        {
            return new ScopeBehavior(scope, value);
        }

        return new ConditionalBehavior(scope, value);
    }
}