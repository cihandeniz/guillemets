namespace Guillemets.Tokens;

internal record CloseBlockToken(string Text, Position Position)
    : IToken;