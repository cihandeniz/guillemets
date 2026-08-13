using Guillemets.Tokens;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Tokenization;

internal class SymbolTree(Func<TokenContext, IToken>? createToken = null)
{
    readonly Dictionary<char, SymbolTree> _children = [];
    Func<TokenContext, IToken>? _createToken = createToken;

    public Func<TokenContext, IToken> CreateToken =>
        _createToken ?? throw new InvalidOperationException("Symbol tree node has no token factory.");

    public SymbolTree Add(ReadOnlySpan<char> path, Func<TokenContext, IToken> createToken,
        bool repeat = false,
        bool newline = false
    )
    {
        var node = AddPath(path, newline ? null : createToken, repeat);
        if (newline)
        {
            node.AddPath([Position.NEWLINE], createToken, false);
        }

        return this;
    }

    SymbolTree AddPath(ReadOnlySpan<char> path, Func<TokenContext, IToken>? createToken, bool repeat)
    {
        if (path.IsEmpty)
        {
            _createToken = createToken;

            return this;
        }

        if (!_children.TryGetValue(path[0], out var child))
        {
            _children[path[0]] = child = new SymbolTree();
        }

        if (repeat && path.Length == 1)
        {
            child.Repeat(path[0]);
        }

        return child.AddPath(path[1..], createToken, repeat);
    }

    void Repeat(char symbol) =>
        _children[symbol] = this;

    public bool TryMatchSymbol(ReadOnlySpan<char> text, [NotNullWhen(true)] out Func<TokenContext, IToken>? createToken, out int length)
    {
        length = 0;
        createToken = null;
        if (text.IsEmpty) { return false; }

        var child = _children.GetValueOrDefault(text[0]);
        if (child is null) { return false; }

        createToken = child.ExtendMatch(text, 1, out length);

        return createToken is not null;
    }

    Func<TokenContext, IToken>? ExtendMatch(ReadOnlySpan<char> text, int index, out int length)
    {
        length = index;
        if (index >= text.Length) { return _createToken; }

        var nextChild = _children.GetValueOrDefault(text[index]);
        if (nextChild is null) { return _createToken; }

        var extended = nextChild.ExtendMatch(text, index + 1, out var extendedLength);
        if (extended is null) { return _createToken; }

        length = extendedLength;

        return extended;
    }
}