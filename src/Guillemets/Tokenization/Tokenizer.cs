namespace Guillemets.Tokenization;

internal class Tokenizer(string _template, SymbolTree _symbolTree)
{
    readonly List<Token> _tokens = [];

    public TokenCursor Tokenize()
    {
        var index = 0;
        var position = new Position(1, 1);
        var pendingStart = 0;
        var pendingPosition = position;
        while (index < _template.Length)
        {
            if (!_symbolTree.TryMatchSymbol(_template.AsSpan(index), position, out var kind, out var length))
            {
                var unmatchedLength = length > 0 ? length : SkipToNextLeadingChar(index);
                position = position.Next(_template.AsSpan(index, unmatchedLength));
                index += unmatchedLength;

                continue;
            }

            FlushPending(pendingStart, index, pendingPosition);

            _tokens.Add(new(kind.Value, _template, index, length, position));
            pendingPosition = position = position.Next(_template.AsSpan(index, length));
            pendingStart = index += length;
        }

        FlushPending(pendingStart, index, pendingPosition);

        return new(_tokens);
    }

    int SkipToNextLeadingChar(int index)
    {
        var next = _template.AsSpan(index + 1).IndexOfAny(_symbolTree.LeadingChars);

        return next < 0 ? _template.Length - index : next + 1;
    }

    void FlushPending(int start, int end, Position position)
    {
        if (end <= start) { return; }

        _tokens.Add(new(_symbolTree.Kind, _template, start, end - start, position));
    }
}