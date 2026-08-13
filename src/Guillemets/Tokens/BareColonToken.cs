namespace Guillemets.Tokens;

internal record BareColonToken(string Text, Position Position)
    : ITextToken;