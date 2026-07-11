using Guillemets.Tokens;

namespace Guillemets;

internal static class Tokenizer
{
    public const char OPEN = '«';
    public const char CLOSE = '»';
    public const char COLON = ':';
    public const char NEWLINE = '\n';
    readonly static HashSet<char> SYMBOLS = [OPEN, CLOSE, COLON];

    public static List<IToken> Tokenize(string template)
    {
        var tokens = new List<IToken>();
        var literalStart = 0;
        var literalStartPosition = new Position(1, 1);
        var position = new Position(1, 1);

        for (var i = 0; i < template.Length; i++)
        {
            var ch = template[i];
            if (SYMBOLS.Contains(ch))
            {
                if (i > literalStart)
                {
                    tokens.Add(new LiteralToken(template[literalStart..i], literalStartPosition));
                }

                tokens.Add(SymbolToken(ch, position));
                literalStart = i + 1;
            }

            position = ch == NEWLINE ? position.NextLine() : position.NextColumn();
            if (literalStart == i + 1) { literalStartPosition = position; }
        }

        if (literalStart < template.Length)
        {
            tokens.Add(new LiteralToken(template[literalStart..], literalStartPosition));
        }

        return tokens;
    }

    static IToken SymbolToken(char ch, Position position)
    {
        if (ch == OPEN) { return new OpenToken(position); }
        if (ch == CLOSE) { return new CloseToken(position); }

        return new ColonToken(position);
    }
}
