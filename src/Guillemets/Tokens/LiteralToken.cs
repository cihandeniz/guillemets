namespace Guillemets.Tokens;

internal record LiteralToken(string Text, Position Position)
    : IToken;