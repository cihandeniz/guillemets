using System.Globalization;

namespace Guillemets.Filters;

public class LowerFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToLower(CultureInfo.CurrentCulture))];
}