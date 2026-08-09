namespace Guillemets;

public record Position(int Line, int Column)
{
    public const char NEWLINE = '\n';

    public Position NextLine(int count = 1) =>
        new(Line + count, 1);

    public Position NextColumn(int count = 1) =>
        new(Line, Column + count);

    public Position Next(char ch) =>
        ch == NEWLINE ? NextLine() : NextColumn();

    public Position Next(ReadOnlySpan<char> text)
    {
        var position = this;
        foreach (var ch in text)
        {
            position = position.Next(ch);
        }

        return position;
    }
}