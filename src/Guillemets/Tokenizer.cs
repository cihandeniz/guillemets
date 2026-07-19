using Guillemets.Tokens;

namespace Guillemets;

internal class Tokenizer(string _template)
{
    public const char OPEN = '«';
    public const char CLOSE = '»';
    public const char COLON = ':';
    public const char NEWLINE = '\n';

    static readonly HashSet<char> SYMBOLS = [OPEN, CLOSE, COLON];

    public TokenCursor Tokenize()
    {
        var tokens = new List<IToken>();
        var literalStart = 0;
        var literalStartPosition = new Position(1, 1);
        var position = new Position(1, 1);

        for (var i = 0; i < _template.Length; i++)
        {
            var ch = _template[i];
            if (SYMBOLS.Contains(ch))
            {
                if (i > literalStart)
                {
                    tokens.Add(new LiteralToken(_template[literalStart..i], literalStartPosition));
                }

                tokens.Add(SymbolToken(ch, position));
                literalStart = i + 1;
            }

            position = ch == NEWLINE ? position.NextLine() : position.NextColumn();
            if (literalStart == i + 1) { literalStartPosition = position; }
        }

        if (literalStart < _template.Length)
        {
            tokens.Add(new LiteralToken(_template[literalStart..], literalStartPosition));
        }

        return new(tokens);
    }

    IToken SymbolToken(char ch, Position position)
    {
        if (ch == OPEN) { return new OpenToken(position); }
        if (ch == CLOSE) { return new CloseToken(position); }

        return new ColonToken(position);
    }
}