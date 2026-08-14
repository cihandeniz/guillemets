using System.Text.RegularExpressions;

namespace Guillemets;

internal static partial class TextCasing
{
    public static string Dehumanize(this string text) =>
        string.Concat(text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    public static string ToLowerWords(this string text) =>
        WordBoundary().Replace(text, " ").ToLowerInvariant();

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}