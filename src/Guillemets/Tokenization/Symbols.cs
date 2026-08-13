using static Guillemets.Position;
using static Guillemets.Tokens.EscapedToken;
using static Guillemets.Tokens.PipeToken;

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

    public static readonly SymbolTree TREE = BuildTree();

    static SymbolTree BuildTree() =>
        new SymbolTree(Tokens.Literal)
            .Add([OPEN], Tokens.Open)
            .Add([OPEN, OPEN], Tokens.OpenBlock, repeat: true)
            .Add([CLOSE], Tokens.Close)
            .Add([CLOSE, CLOSE], Tokens.CloseBlock, repeat: true, newline: true)
            .Add([BACKSLASH, OPEN], Tokens.Escaped)
            .Add([BACKSLASH, CLOSE], Tokens.Escaped)
            .Add([BACKSLASH, BACKSLASH], Tokens.Escaped)
            .Add([BACKSLASH, TILDE], Tokens.Escaped)
            .Add([COLON, SPACE], Tokens.Colon)
            .Add([COLON], Tokens.BareColon)
            .Add([SPACE, DELIMITER, SPACE], Tokens.Pipe)
            .Add([TILDE], Tokens.Else, newline: true)
            .Add([BANG], Tokens.Negation)
            .Add([EQUALS], Tokens.Equals)
            .Add([NEWLINE], Tokens.Newline);
}