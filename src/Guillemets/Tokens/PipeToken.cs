namespace Guillemets.Tokens;

internal record PipeToken(string Text, Position Position)
    : ITextToken;