using static Guillemets.Position;

namespace Guillemets.Filters;

/// <summary>
/// Strips the newlines around a value, leaving the text inside it alone.
/// As a block footer this drops the blank line each iteration would
/// otherwise be padded with, so the block renders as a single paragraph
/// instead of one paragraph per item.
/// </summary>
public class TrimFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        [.. values.Select(value => value.Trim(NEWLINE))];
}