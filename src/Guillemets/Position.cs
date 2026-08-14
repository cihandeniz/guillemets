namespace Guillemets;

public record Position(int Line, int Column)
{
    public const char NEWLINE = '\n';
    public const char TAB = '\t';

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