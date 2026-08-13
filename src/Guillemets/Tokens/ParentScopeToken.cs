namespace Guillemets.Tokens;

internal record ParentScopeToken(string Text, Position Position)
    : ITextToken;