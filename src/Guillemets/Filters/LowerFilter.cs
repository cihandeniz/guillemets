using System.Globalization;

namespace Guillemets.Filters;

/// <summary>
/// <c>string.ToLower(CultureInfo.CurrentCulture)</c> — casing follows the
/// host's ambient culture.
/// </summary>
public class LowerFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToLower(CultureInfo.CurrentCulture))];
}