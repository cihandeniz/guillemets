using System.Globalization;

namespace Guillemets.Filters;

public class CurrencyFilter(
    string? currencySymbol = null
) : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    string Format(string value, string? arg)
    {
        var numberFormat = currencySymbol is null
            ? CultureInfo.CurrentCulture.NumberFormat
            : WithSymbol(currencySymbol);

        return decimal.Parse(value, CultureInfo.InvariantCulture).ToString(string.IsNullOrWhiteSpace(arg) ? "C" : arg.Trim(), numberFormat);
    }

    static NumberFormatInfo WithSymbol(string symbol)
    {
        var numberFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        numberFormat.CurrencySymbol = symbol;

        return numberFormat;
    }
}