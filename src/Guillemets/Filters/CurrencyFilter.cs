using System.Globalization;

namespace Guillemets.Filters;

/// <summary>
/// Parses with <c>decimal.Parse</c> (invariant culture) and formats
/// against the ambient culture's currency convention — symbol, symbol
/// placement, and default decimal count all come from the culture. The
/// argument overrides the decimal count (<c>C0</c>/<c>C3</c>, ...) while
/// keeping the culture's own symbol/placement; with no argument, uses
/// .NET's standard <c>"C"</c> format.
/// </summary>
/// <param name="currencySymbol">
/// Fixes the currency symbol regardless of ambient culture, while still
/// letting the culture drive digit grouping/decimal separators. Leave
/// <see langword="null"/> to use the culture's own symbol too.
/// </param>
public class CurrencyFilter(
    string? currencySymbol = null
) : IFilter
{
    /// <inheritdoc/>
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