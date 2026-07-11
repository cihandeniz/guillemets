namespace Guillemets;

public sealed record Position(int Line, int Column)
{
    public Position NextLine() =>
        new(Line + 1, 1);

    public Position NextColumn() =>
        new(Line, Column + 1);
}
