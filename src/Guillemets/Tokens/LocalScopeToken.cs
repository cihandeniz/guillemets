namespace Guillemets.Tokens;

internal record LocalScopeToken(string Text, Position Position)
    : ITextToken;