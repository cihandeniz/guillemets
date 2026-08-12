namespace Guillemets.Filters;

public interface IFilter
{
    IEnumerable<string> Apply(IEnumerable<string> values, string? arg);

    string? GetDefaultArg(FilterContext context) =>
        null;
}