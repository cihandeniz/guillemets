using System.Globalization;

namespace Guillemets.Filters;

internal class LowerFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToLower(CultureInfo.CurrentCulture))];
}