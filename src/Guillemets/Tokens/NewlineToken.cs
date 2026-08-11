namespace Guillemets.Tokens;

internal record NewlineToken(string Text, Position Position)
    : ITextToken;