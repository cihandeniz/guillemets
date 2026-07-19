using Guillemets.Tokens;

using static Guillemets.Tokenizer;

namespace Guillemets;

internal class TokenCursor(List<IToken> _tokens)
{
    int _position;

    public bool AtEnd => _position >= _tokens.Count;
    public IToken Current => _tokens[_position];

    public void Advance() =>
        _position++;

    public void Skip(int count) =>
        _position += count;

    public int CountConsecutive<TToken>()
        where TToken : IToken
    {
        var count = 0;
        while (_position + count < _tokens.Count && _tokens[_position + count] is TToken)
        {
            count++;
        }

        return count;
    }

    public void TrimCurrentLiteral(int length)
    {
        var literal = (LiteralToken)Current;
        _tokens[_position] = literal with { Text = literal.Text[length..] };
    }

    public void TrimLeadingNewlineIfPresent()
    {
        if (AtEnd) { return; }
        if (Current is not LiteralToken literal) { return; }
        if (literal.Text.Length == 0 || literal.Text[0] != NEWLINE) { return; }

        TrimCurrentLiteral(1);
    }
}