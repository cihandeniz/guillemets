using Guillemets.Data;
using Guillemets.Filters;
using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record VariableNode(PropertyChainNode Properties, IReadOnlyList<FilterNode> Filters)
    : IRenderable
{
    const string DEFAULT_JOIN = ", ";

    static IEnumerable<string> AsDisplayStrings(IDataSource value) =>
        value.Kind == DataKind.Array
            ? value.EnumerateArray().Select(item => NormalizeNewlines(item.AsDisplayString()))
            : [NormalizeNewlines(value.AsDisplayString())];

    static string NormalizeNewlines(string? text)
    {
        if (text is null || !text.Contains('\r')) { return text ?? ""; }

        return text.Replace("\r\n", NEWLINE.ToString()).Replace('\r', NEWLINE);
    }

    public string Render(RenderContext context, Scope scope)
    {
        var values = context.PropertyResolver.Resolve(scope, Properties).SelectMany(AsDisplayStrings);
        foreach (var filter in Filters)
        {
            values = filter.Apply(values, FilterContext.Inline);
        }

        return string.Join(DEFAULT_JOIN, values);
    }
}