namespace Guillemets.Filters;

internal class JoinFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        values.Any() ? [string.Join(arg, values)] : values;
}