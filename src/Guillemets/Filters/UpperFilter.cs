using System.Globalization;

namespace Guillemets.Filters;

public class UpperFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToUpper(CultureInfo.CurrentCulture))];
}