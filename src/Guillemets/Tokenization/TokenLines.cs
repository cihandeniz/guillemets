using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal class TokenLines(List<Token> _tokens)
{
    TokenLine? _recent;

    public TokenLine At(int index)
    {
        if (_recent is { } recent && recent.Covers(index)) { return recent; }

        var line = Build(StartOf(index));
        _recent = line;

        return line;
    }

    public TokenLine? Above(TokenLine line) =>
        line.Start == 0 ? null : Build(StartOf(line.Start - 1));

    public TokenLine? Below(TokenLine line) =>
        line.End >= _tokens.Count ? null : Build(line.End);

    TokenLine Build(int start)
    {
        var content = start;
        while (content < _tokens.Count && _tokens[content].Kind is Quote) { content++; }

        var end = content;
        while (end < _tokens.Count && !_tokens[end].TerminatesLine) { end++; }

        var blank = end == content;
        if (end < _tokens.Count) { end++; }

        return new(start, end, content - start, blank);
    }

    int StartOf(int index)
    {
        while (index > 0 && !_tokens[index - 1].TerminatesLine) { index--; }

        return index;
    }
}