using Guillemets.Tokens;

namespace Guillemets.Tokenization;

internal class TokenCursor(List<IToken> _tokens)
{
    int _position;

    public bool AtEnd => _position >= _tokens.Count;
    public IToken Current => _tokens[_position];
    public int Position => _position;

    public void Advance() =>
        _position++;

    public void Rewind(int position) =>
        _position = position;

    public void ReplaceCurrent(IToken token) =>
        _tokens[_position] = token;
}