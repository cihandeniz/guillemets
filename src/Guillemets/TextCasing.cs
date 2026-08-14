using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Guillemets;

internal static partial class TextCasing
{
    static readonly ConcurrentDictionary<string, string> DEHUMANIZE_CACHE = new();

    public static string Dehumanize(this string text) =>
        DEHUMANIZE_CACHE.GetOrAdd(text, static t =>
            string.Concat(t.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
            )
        );

    public static string ToLowerWords(this string text) =>
        WordBoundary().Replace(text, " ").ToLowerInvariant();

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}