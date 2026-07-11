using static Guillemets.Tokens;

namespace Guillemets;

internal static class Tokenizer
{
    const char OPEN = '«';
    const char CLOSE = '»';
    const char COLON = ':';
    readonly static HashSet<char> CHARS = [OPEN, CLOSE, COLON];

    public static List<IToken> Tokenize(string template)
    {
        var tokens = new List<IToken>();
        var literalStart = 0;
        for (var i = 0; i < template.Length; i++)
        {
            var ch = template[i];
            if (!CHARS.Contains(ch)) { continue; }

            if (i > literalStart)
            {
                tokens.Add(new LiteralToken(template[literalStart..i]));
            }

            tokens.Add(SymbolToken(ch));
            literalStart = i + 1;
        }

        if (literalStart < template.Length)
        {
            tokens.Add(new LiteralToken(template[literalStart..]));
        }

        return tokens;
    }

    static IToken SymbolToken(char ch)
    {
        if (ch == OPEN) { return new OpenToken(); }
        if (ch == CLOSE) { return new CloseToken(); }

        return new ColonToken();
    }
}
