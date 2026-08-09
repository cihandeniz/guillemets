namespace Guillemets.Tokens;

internal record ElseToken(string Text, Position Position)
    : IToken;