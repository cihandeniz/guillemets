using System.Globalization;

namespace Guillemets.Filters;

public class NumberFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    static string Format(string value, string? arg) =>
        decimal.Parse(value, CultureInfo.InvariantCulture).ToString(arg?.Trim(), CultureInfo.CurrentCulture);
}