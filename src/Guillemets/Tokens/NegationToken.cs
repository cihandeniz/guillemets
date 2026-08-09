namespace Guillemets.Tokens;

internal record NegationToken(string Text, Position Position)
    : ITextToken;