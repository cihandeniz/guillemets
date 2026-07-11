namespace Guillemets;

internal static class Tokens
{
    internal interface IToken;
    internal sealed record OpenToken : IToken;
    internal sealed record CloseToken : IToken;
    internal sealed record ColonToken : IToken;
    internal sealed record LiteralToken(string Text) : IToken;
}
