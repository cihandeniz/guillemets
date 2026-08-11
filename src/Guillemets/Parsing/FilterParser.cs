using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class FilterParser(TokenCursor _tokens)
    : IParser
{
    public INode Parse(IToken token)
    {
        if (!TryParse(out var filter))
        {
            throw new TemplateParseException("Expected a filter in the form (name = value)", token.Position);
        }

        return filter;
    }

    public bool TryParse([NotNullWhen(true)] out FilterNode? filter)
    {
        filter = null;
        var start = _tokens.Position;

        if (_tokens.Current is not OpenParenToken) { return false; }
        _tokens.Advance();

        if (!TryReadUntil<EqualsToken>(out var rawName) || !TryReadUntil<CloseParenToken>(out var rawValue))
        {
            _tokens.Rewind(start);

            return false;
        }

        filter = new FilterNode(rawName.Trim(), rawValue.TrimStart());

        return true;
    }

    bool TryReadUntil<TTerminator>(out string text) where TTerminator : IToken
    {
        var builder = new StringBuilder();
        while (true)
        {
            if (_tokens.AtEnd) { text = ""; return false; }
            if (_tokens.Current is TTerminator)
            {
                _tokens.Advance();
                text = builder.ToString();

                return true;
            }

            if (_tokens.Current is not LiteralToken literal || literal.Text.Contains(NEWLINE)) { text = ""; return false; }

            builder.Append(literal.Text);
            _tokens.Advance();
        }
    }
}
