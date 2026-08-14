using System.Globalization;

namespace Guillemets.Filters;

public class TruncateFilter : IFilter
{
    const char ELLIPSIS = '…';

    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => Truncate(value, arg))];

    static string Truncate(string value, string? arg)
    {
        var maxLength = ParseMaxLength(arg);
        if (value.Length <= maxLength) { return value; }

        return $"{value[..CutPoint(value, maxLength)]}{ELLIPSIS}";
    }

    static int ParseMaxLength(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            throw new InvalidOperationException("The 'truncate' filter requires a numeric argument, e.g. 'truncate: 20'");
        }

        if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxLength))
        {
            throw new InvalidOperationException($"The 'truncate' filter's argument must be a whole number, not '{arg}'");
        }

        return maxLength;
    }

    static int CutPoint(string value, int maxLength)
    {
        var splitsSurrogatePair = maxLength > 0 && maxLength < value.Length &&
            char.IsHighSurrogate(value[maxLength - 1]) && char.IsLowSurrogate(value[maxLength]);

        return splitsSurrogatePair ? maxLength - 1 : maxLength;
    }
}