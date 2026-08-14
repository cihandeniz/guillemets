using System.Globalization;

namespace Guillemets.Filters;

/// <summary>
/// Parses with <c>DateTime.Parse</c> (invariant culture) and formats back
/// out using .NET custom date-and-time format strings (e.g.
/// <c>dd/MM/yyyy</c>) against the ambient culture at render time.
/// </summary>
public class DateFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Format(value, arg))];

    static string Format(string value, string? arg) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture).ToString(arg?.Trim(), CultureInfo.CurrentCulture);
}