using static Guillemets.Position;

namespace Guillemets.Tokenization;

internal static class Symbols
{
    const char OPEN = '«';
    const char CLOSE = '»';
    const char COLON = ':';
    const char TILDE = '~';
    const char BANG = '!';
    const char EQUALS = '=';
    const char OPEN_PAREN = '(';
    const char CLOSE_PAREN = ')';

    public static readonly SymbolTree TREE = BuildTree();

    static SymbolTree BuildTree() =>
        new SymbolTree(Tokens.Literal)
            .Add([OPEN], Tokens.Open)
            .Add([OPEN, OPEN], Tokens.OpenBlock, repeat: true)
            .Add([CLOSE], Tokens.Close)
            .Add([CLOSE, CLOSE], Tokens.CloseBlock, repeat: true, newline: true)
            .Add([COLON], Tokens.Colon)
            .Add([TILDE], Tokens.Else, newline: true)
            .Add([BANG], Tokens.Negation)
            .Add([EQUALS], Tokens.Equals)
            .Add([OPEN_PAREN], Tokens.OpenParen)
            .Add([CLOSE_PAREN], Tokens.CloseParen)
            .Add([NEWLINE], Tokens.Newline);
}