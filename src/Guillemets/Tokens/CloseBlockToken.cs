namespace Guillemets.Tokens;

internal record CloseBlockToken(string Text, Position Position)
    : IToken
{
    public int Depth => Text.Count(ch => ch != Position.NEWLINE);
}