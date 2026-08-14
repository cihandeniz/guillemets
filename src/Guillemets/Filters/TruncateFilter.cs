using System.Globalization;

namespace Guillemets.Filters;

public class TruncateFilter : IFilter
{
    const char ELLIPSIS = '…';

    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Truncate(value, arg))];

    static string Truncate(string value, string? arg)
    {
        var maxLength = int.Parse(
            arg ?? throw new InvalidOperationException("truncate filter requires a value"),
            CultureInfo.InvariantCulture
        );

        return value.Length <= maxLength ? value : $"{value[..maxLength]}{ELLIPSIS}";
    }
}