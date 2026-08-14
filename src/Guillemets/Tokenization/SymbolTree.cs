using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Tokenization;

internal class SymbolTree(TokenKind? kind = null)
{
    readonly Dictionary<char, SymbolTree> _children = [];
    TokenKind? _kind = kind;

    public TokenKind Kind =>
        _kind ?? throw new InvalidOperationException("Symbol tree node has no token kind.");

    public SymbolTree Add(ReadOnlySpan<char> path, TokenKind kind,
        bool repeat = false,
        bool newline = false
    )
    {
        var node = AddPath(path, newline ? null : kind, repeat);
        if (newline)
        {
            node.AddPath([Position.NEWLINE], kind, false);
        }

        return this;
    }

    SymbolTree AddPath(ReadOnlySpan<char> path, TokenKind? kind, bool repeat)
    {
        if (path.IsEmpty)
        {
            _kind = kind;

            return this;
        }

        if (!_children.TryGetValue(path[0], out var child))
        {
            _children[path[0]] = child = new();
        }

        if (repeat && path.Length == 1)
        {
            child.Repeat(path[0]);
        }

        return child.AddPath(path[1..], kind, repeat);
    }

    void Repeat(char symbol) =>
        _children[symbol] = this;

    public bool TryMatchSymbol(ReadOnlySpan<char> text, [NotNullWhen(true)] out TokenKind? kind, out int length)
    {
        length = 0;
        kind = null;
        if (text.IsEmpty) { return false; }

        var child = _children.GetValueOrDefault(text[0]);
        if (child is null) { return false; }

        kind = child.ExtendMatch(text, 1, out length);

        return kind is not null;
    }

    TokenKind? ExtendMatch(ReadOnlySpan<char> text, int index, out int length)
    {
        length = index;
        if (index >= text.Length) { return _kind; }

        var nextChild = _children.GetValueOrDefault(text[index]);
        if (nextChild is null) { return _kind; }

        var extended = nextChild.ExtendMatch(text, index + 1, out var extendedLength);
        if (extended is null) { return _kind; }

        length = extendedLength;

        return extended;
    }
}