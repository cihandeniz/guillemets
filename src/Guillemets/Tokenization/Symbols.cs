namespace Guillemets.Tokenization;

internal static class Symbols
{
    public const char OPEN = '«';
    public const char CLOSE = '»';
    public const char COLON = ':';
    public const char TILDE = '~';
    public const char BANG = '!';
    public const char EQUALS = '=';

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
            .Add([EQUALS], Tokens.Equals);
}