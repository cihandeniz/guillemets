namespace Guillemets.Tokens;

internal record EqualsToken(string Text, Position Position)
    : ITextToken;
