namespace Guillemets.Tokens;

internal sealed record LiteralToken(string Text, Position Position)
    : IToken;
