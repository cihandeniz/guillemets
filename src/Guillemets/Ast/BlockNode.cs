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
    string QuoteMarker = "",
    int QuoteDepth = 0,
    bool BlankLineAfterClose = false
) : IRenderable
{
    readonly TableBody? _table = TableBody.From(Body, QuoteDepth);

    string BlankLine => NEWLINE + QuoteMarker + NEWLINE;
    string BlankLineSeparator => QuoteMarker + NEWLINE;

    string WithoutTrailingBlankLine(string item) =>
        item.EndsWith(BlankLine, StringComparison.Ordinal) ? item[..^BlankLineSeparator.Length] : item;

    bool SpansParagraphs(string body) =>
        body.Contains(BlankLine, StringComparison.Ordinal);

    public string Render(RenderContext context, Scope scope)
    {
        var items = ResolveBehavior(context, scope).Render(context, Body, ElseBody);
        var rendered = JoinItems(items);
        if (VariableName is null) { return RestoreBlankLineAfterClose(rendered); }

        scope.DefineVariable(VariableName, rendered.TrimEnd(NEWLINE));

        return string.Empty;
    }

    string RestoreBlankLineAfterClose(string rendered) =>
        BlankLineAfterClose && rendered.Length > 0 ? rendered + BlankLineSeparator : rendered;

    string JoinItems(IEnumerable<string> items)
    {
        if (Footer is { Count: > 0 }) { return string.Concat(ApplyFooter(items)); }

        var bodies = items.Select(WithoutTrailingBlankLine).ToList();

        return string.Join(bodies.Exists(SpansParagraphs) ? BlankLineSeparator : string.Empty, bodies);
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