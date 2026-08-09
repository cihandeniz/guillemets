namespace Guillemets.Tokens;

internal record ColonToken(string Text, Position Position)
    : ITextToken;