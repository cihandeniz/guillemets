namespace Guillemets.Filters;

internal class DefaultFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg)
    {
        arg ??= string.Empty;

        var replaced = values.Select(value => string.IsNullOrEmpty(value) ? arg : value).ToList();

        return replaced.Count == 0 ? [arg] : replaced;
    }
}