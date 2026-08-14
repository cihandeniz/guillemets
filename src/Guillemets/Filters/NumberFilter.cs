using System.Globalization;

namespace Guillemets.Filters;

/// <summary>
/// Parses with <c>decimal.Parse</c> (invariant culture) and formats back
/// out against the ambient culture using a .NET standard or custom
/// numeric format string given as its argument (e.g. <c>N2</c>) — the
/// same primitive <see cref="CurrencyFilter"/> wraps, minus the fixed
/// <c>"N2"</c> and the currency symbol. With no argument, uses .NET's
/// default general format.
/// </summary>
public class NumberFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    static string Format(string value, string? arg) =>
        decimal.Parse(value, CultureInfo.InvariantCulture).ToString(arg?.Trim(), CultureInfo.CurrentCulture);
}