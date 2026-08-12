using System.Globalization;

namespace Guillemets.Filters;

internal class CurrencyFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    static string Format(string value, string? arg) =>
        $"{arg}{decimal.Parse(value, CultureInfo.InvariantCulture).ToString("N2", CultureInfo.InvariantCulture)}";
}