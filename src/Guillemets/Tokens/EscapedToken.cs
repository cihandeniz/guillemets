namespace Guillemets.Tokens;

internal record EscapedToken(string Text, Position Position)
    : LiteralToken(Text, Position)
{
    internal const char BACKSLASH = '\\';
}