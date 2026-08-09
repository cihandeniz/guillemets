using Guillemets.Tokens;

namespace Guillemets.Tokenization;

internal class TokenCursor(List<IToken> _tokens)
{
    int _position;

    public bool AtEnd => _position >= _tokens.Count;
    public IToken Current => _tokens[_position];

    public void Advance() =>
        _position++;

    public void TrimCurrentLiteral(int length)
    {
        var literal = (LiteralToken)Current;
        _tokens[_position] = literal with { Text = literal.Text[length..] };
    }
}