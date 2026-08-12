using System.Globalization;

namespace Guillemets.Filters;

internal class DateFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    static string Format(string value, string? arg) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture).ToString(arg?.Trim(), CultureInfo.InvariantCulture);
}