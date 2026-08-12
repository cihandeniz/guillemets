using Guillemets.Data;
using Guillemets.Filters;
using Guillemets.Rendering;

namespace Guillemets.Ast;

internal record VariableNode(PropertyChainNode Properties, IReadOnlyList<FilterNode> Filters)
    : IRenderable
{
    const string DEFAULT_JOIN = ", ";

    public string Render(RenderContext context, Scope scope)
    {
        var values = context.PropertyResolver.Resolve(scope, Properties).SelectMany(AsDisplayStrings);
        foreach (var filter in Filters)
        {
            values = filter.Apply(values, FilterContext.Inline);
        }

        return string.Join(DEFAULT_JOIN, values);
    }

    static IEnumerable<string> AsDisplayStrings(IDataSource value) =>
        value.Kind == DataKind.Array
            ? value.EnumerateArray().Select(item => item.AsDisplayString() ?? "")
            : [value.AsDisplayString() ?? ""];
}