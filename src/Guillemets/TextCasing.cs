using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Guillemets;

internal static partial class TextCasing
{
    static readonly ConcurrentDictionary<string, string> DEHUMANIZE_CACHE = new();

    extension(string text)
    {
        public string Dehumanize() =>
            DEHUMANIZE_CACHE.GetOrAdd(text, static t =>
                string.Concat(t.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
                )
            );

        public string ToLowerWords() =>
            WordBoundary().Replace(text, " ").ToLowerInvariant();
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}