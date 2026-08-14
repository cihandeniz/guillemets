namespace Guillemets.Tokenization;

internal class TokenCursor(List<Token> _tokens)
{
    int _position;

    public bool AtEnd => _position >= _tokens.Count;
    public Token Current => _tokens[_position];
    public int Position => _position;

    public void Advance() =>
        _position++;

    public void Rewind(int position) =>
        _position = position;

    public bool LineReaches(TokenKind kind)
    {
        for (var i = _position; i < _tokens.Count; i++)
        {
            var current = _tokens[i].Kind;
            if (current == kind) { return true; }
            if (current == TokenKind.Newline) { return false; }
        }

        return false;
    }
}