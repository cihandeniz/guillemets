using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal static class Symbols
{
    const char OPEN = '«';
    internal const char CLOSE = '»';
    const char COLON = ':';
    internal const char TILDE = '~';
    const char BANG = '!';
    const char EQUALS = '=';
    internal const char SPACE = ' ';
    internal const char QUOTE = '>';
    internal const char PIPE = '|';
    const char DOT = '.';
    internal const char BACKSLASH = '\\';
    internal const char SLASH = '/';

    public static readonly SymbolTree TREE = BuildTree();

    static SymbolTree BuildTree() =>
        new SymbolTree(Literal)
            .Add([OPEN], Open)
            .Add([OPEN, OPEN], OpenBlock, repeat: true)
            .Add([OPEN, OPEN, TILDE], OpenBlock)
            .Add([CLOSE], Close)
            .Add([CLOSE, CLOSE], CloseBlock, repeat: true)
            .Add([TILDE, CLOSE, CLOSE], CloseBlock, repeat: true)
            .Add([BACKSLASH, OPEN], Escaped)
            .Add([BACKSLASH, CLOSE], Escaped)
            .Add([BACKSLASH, BACKSLASH], Escaped)
            .Add([BACKSLASH, TILDE], Escaped)
            .Add([COLON, SPACE], Colon)
            .Add([COLON, NEWLINE], Colon)
            .Add([COLON], BareColon)
            .Add([DOT, COLON, SPACE], LocalScope)
            .Add([DOT, COLON, NEWLINE], LocalScope)
            .Add([DOT, DOT, COLON, SPACE], ParentScope)
            .Add([DOT, DOT, COLON, NEWLINE], ParentScope)
            .Add([SPACE, SLASH, SPACE], FilterDelimiter)
            .Add([SPACE, SLASH, NEWLINE], FilterDelimiter)
            .Add([NEWLINE, SLASH, SPACE], FilterDelimiter)
            .Add([NEWLINE, SLASH, NEWLINE], FilterDelimiter)
            .Add([TILDE], Else)
            .Add([BANG], Negation)
            .Add([EQUALS], Assign)
            .Add([QUOTE], Quote)
            .Add([QUOTE, SPACE], Quote)
            .Add([PIPE], Pipe)
            .Add([PIPE, SPACE], Pipe)
            .Add([NEWLINE], Newline)
            .Add([NEWLINE, NEWLINE], Newline, repeat: true, limitRepeat: false);
}