namespace Guillemets.Tokens;

internal record OpenParenToken(string Text, Position Position)
    : ITextToken;