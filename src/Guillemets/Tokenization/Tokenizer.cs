using Guillemets.Tokens;

namespace Guillemets.Tokenization;

internal class Tokenizer(string _template, SymbolTree _symbolTree)
{
    readonly List<IToken> _tokens = [];

    public TokenCursor Tokenize()
    {
        var index = 0;
        var position = new Position(1, 1);
        var pendingStart = 0;
        var pendingPosition = position;
        while (index < _template.Length)
        {
            if (!_symbolTree.TryMatchSymbol(_template.AsSpan(index), out var createToken, out var length))
            {
                position = position.Next(_template[index]);
                index++;

                continue;
            }

            FlushPending(pendingStart, index, pendingPosition);

            _tokens.Add(createToken(new(_template[index..(index + length)], position)));
            pendingPosition = position = position.Next(_template.AsSpan(index, length));
            pendingStart = index += length;
        }

        FlushPending(pendingStart, index, pendingPosition);

        return new(_tokens);
    }

    void FlushPending(int start, int end, Position position)
    {
        if (end <= start) { return; }

        _tokens.Add(_symbolTree.CreateToken(new(_template[start..end], position)));
    }
}