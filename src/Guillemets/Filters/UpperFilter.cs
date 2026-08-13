using System.Globalization;

namespace Guillemets.Filters;

internal class UpperFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToUpper(CultureInfo.CurrentCulture))];
}