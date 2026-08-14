using System.Globalization;

namespace Guillemets.Filters;

/// <summary>
/// <c>string.ToUpper(CultureInfo.CurrentCulture)</c> — casing follows the
/// host's ambient culture, e.g. Turkish <c>tr-TR</c> maps <c>i</c> to
/// <c>İ</c> (not <c>I</c>) under this filter.
/// </summary>
public class UpperFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.ToUpper(CultureInfo.CurrentCulture))];
}