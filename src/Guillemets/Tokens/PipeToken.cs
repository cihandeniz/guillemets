namespace Guillemets.Tokens;

internal record PipeToken(string Text, Position Position)
    : ITextToken
{
    internal const char DELIMITER = '|';
}