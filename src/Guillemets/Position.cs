namespace Guillemets;

/// <summary>
/// A 1-based line/column location in the original template source, used
/// to point at where a parse error occurred.
/// </summary>
/// <param name="Line">The 1-based line number.</param>
/// <param name="Column">The 1-based column number.</param>
public record Position(int Line, int Column)
{
    /// <summary>
    /// The newline character templates are normalized to internally,
    /// regardless of the source's original line ending.
    /// </summary>
    public const char NEWLINE = '\n';

    /// <summary>
    /// The tab character, as it appears in raw template source.
    /// </summary>
    public const char TAB = '\t';

    /// <summary>
    /// Whether this position is the first column of its line.
    /// </summary>
    public bool AtLineStart =>
        Column == 1;

    internal Position NextLine(int count = 1) =>
        new(Line + count, 1);

    internal Position NextColumn(int count = 1) =>
        new(Line, Column + count);

    internal Position Next(char ch) =>
        ch == NEWLINE ? NextLine() : NextColumn();

    internal Position Next(ReadOnlySpan<char> text)
    {
        var position = this;
        foreach (var ch in text)
        {
            position = position.Next(ch);
        }

        return position;
    }
}