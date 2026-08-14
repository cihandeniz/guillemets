using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal static class Symbols
{
    const char OPEN = '«';
    const char CLOSE = '»';
    const char COLON = ':';
    const char TILDE = '~';
    const char BANG = '!';
    const char EQUALS = '=';
    const char SPACE = ' ';
    const char DOT = '.';
    internal const char BACKSLASH = '\\';
    internal const char SLASH = '/';

    public static readonly SymbolTree TREE = BuildTree();

    static SymbolTree BuildTree() =>
        new SymbolTree(Literal)
            .Add([OPEN], Open)
            .Add([OPEN, OPEN], OpenBlock, repeat: true)
            .Add([CLOSE], Close)
            .Add([CLOSE, CLOSE], CloseBlock, repeat: true, newline: true)
            .Add([BACKSLASH, OPEN], Escaped)
            .Add([BACKSLASH, CLOSE], Escaped)
            .Add([BACKSLASH, BACKSLASH], Escaped)
            .Add([BACKSLASH, TILDE], Escaped)
            .Add([COLON, SPACE], Colon)
            .Add([COLON], BareColon)
            .Add([DOT, COLON, SPACE], LocalScope)
            .Add([DOT, DOT, COLON, SPACE], ParentScope)
            .Add([SPACE, SLASH, SPACE], FilterDelimiter)
            .Add([TILDE], Else, newline: true)
            .Add([BANG], Negation)
            .Add([EQUALS], Assign)
            .Add([NEWLINE], Newline);
}