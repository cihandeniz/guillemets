namespace Guillemets.Tokens;

internal record OpenBlockToken(string Text, Position Position)
    : IToken
{
    public int Depth => Text.Length;
}