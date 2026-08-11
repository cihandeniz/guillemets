namespace Guillemets.Tokens;

internal record CloseParenToken(string Text, Position Position)
    : ITextToken;