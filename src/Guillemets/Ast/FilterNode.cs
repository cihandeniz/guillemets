using Guillemets.Filters;

namespace Guillemets.Ast;

internal record FilterNode(IFilter Filter, string? Arg)
{
    internal IEnumerable<string> Apply(IEnumerable<string> values, FilterContext context) =>
        Filter.Apply(values, Arg ?? Filter.GetDefaultArg(context));
}