using Guillemets.Filters;

namespace Guillemets.Ast;

internal record FilterNode(IFilter Filter, string? Arg, Position Position)
{
    internal IEnumerable<string> Apply(IEnumerable<string> values, FilterContext context)
    {
        try
        {
            return Filter.Apply(values, Arg ?? Filter.GetDefaultArg(context)).ToList();
        }
        catch (Exception ex) when (ex is not TemplateParseException)
        {
            throw new TemplateParseException(ex.Message, Position);
        }
    }
}