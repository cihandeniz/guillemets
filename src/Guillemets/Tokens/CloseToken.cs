namespace Guillemets.Tokens;

internal record CloseToken(string Text, Position Position)
    : ITextToken;