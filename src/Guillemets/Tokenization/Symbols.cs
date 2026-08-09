namespace Guillemets.Tokenization;

internal static class Symbols
{
    public const char OPEN = '«';
    public const char CLOSE = '»';
    public const char COLON = ':';
    public const char DASH = '-';

    public static readonly SymbolTree TREE = BuildTree();

    static SymbolTree BuildTree() =>
        new SymbolTree(Tokens.Literal)
            .Add([OPEN], Tokens.Open)
            .Add([OPEN, OPEN], Tokens.OpenBlock)
            .Add([CLOSE], Tokens.Close)
            .Add([CLOSE, CLOSE], Tokens.CloseBlock)
            .Add([COLON], Tokens.Colon)
            .Add([DASH, DASH, Position.NEWLINE], Tokens.Else);
}