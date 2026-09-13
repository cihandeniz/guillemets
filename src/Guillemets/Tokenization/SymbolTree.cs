using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Tokenization;

internal class SymbolTree(TokenKind? kind = null)
{
    internal const int MAX_REPEAT = 7;

    readonly Dictionary<char, SymbolTree> _children = [];
    TokenKind? _kind = kind;
    SearchValues<char>? _leadingChars;
    bool _repeatLimited;

    public SearchValues<char> LeadingChars =>
        _leadingChars ??= SearchValues.Create([.. _children.Keys]);

    public TokenKind Kind =>
        _kind ?? throw new InvalidOperationException("Symbol tree node has no token kind.");

    public SymbolTree Add(ReadOnlySpan<char> path, TokenKind kind,
        bool repeat = false,
        bool limitRepeat = true
    )
    {
        AddPath(path, kind, repeat, limitRepeat);

        return this;
    }

    void AddPath(ReadOnlySpan<char> path, TokenKind kind, bool repeat, bool limitRepeat)
    {
        if (path.IsEmpty)
        {
            _kind = kind;

            return;
        }

        if (!_children.TryGetValue(path[0], out var child))
        {
            _children[path[0]] = child = new();
        }

        if (repeat && path.Length == 1)
        {
            child.Repeat(path[0], limitRepeat);
        }

        child.AddPath(path[1..], kind, repeat, limitRepeat);
    }

    void Repeat(char symbol, bool limited)
    {
        _children[symbol] = this;
        _repeatLimited = limited;
    }

    public bool TryMatchSymbol(ReadOnlySpan<char> text, Position position, [NotNullWhen(true)] out TokenKind? kind, out int length)
    {
        length = 0;
        kind = null;
        if (text.IsEmpty) { return false; }

        var child = _children.GetValueOrDefault(text[0]);
        if (child is null) { return false; }

        kind = child.ExtendMatch(text, 1, position, out length);

        return kind is not null;
    }

    TokenKind? ExtendMatch(ReadOnlySpan<char> text, int index, Position startPosition, out int length)
    {
        length = index;
        if (index >= text.Length) { return _kind; }

        var nextChild = _children.GetValueOrDefault(text[index]);
        if (nextChild is null) { return _kind; }

        if (ReferenceEquals(nextChild, this) && _repeatLimited && index + 1 > MAX_REPEAT)
        {
            throw new TemplateParseException(
                $"A run of the same guillemet may not exceed {MAX_REPEAT} deep - reuse a depth instead of nesting further",
                startPosition
            );
        }

        var extended = nextChild.ExtendMatch(text, index + 1, startPosition, out var extendedLength);
        if (extended is null) { return _kind; }

        length = extendedLength;

        return extended;
    }
}